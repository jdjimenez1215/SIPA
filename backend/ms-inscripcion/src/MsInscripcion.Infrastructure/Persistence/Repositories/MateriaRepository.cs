using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;
using Npgsql;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class MateriaRepository(InscripcionDbContext context) : IMateriaRepository
{
    public async Task<IReadOnlyList<Materia>> LockByIdsAsync(
        IReadOnlyCollection<int> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToArray();

        // Step 1: pessimistic row locks, ascending id (deadlock avoidance). Scalar query, no composition over FOR UPDATE.
        // Deadlocks surface here, not in SaveChanges, so translate them the same way (409 CONFLICTO_CONCURRENCIA).
        try
        {
            await context.Database
                .SqlQuery<int>($"SELECT id AS \"Value\" FROM materias WHERE id = ANY({ids}) ORDER BY id FOR UPDATE")
                .ToListAsync(ct);
        }
        catch (PostgresException pg)
        {
            throw UnitOfWork.Translate(pg, pg);
        }

        // Step 2: regular load of the (now locked) rows with their graph.
        return await context.Materias
            .Include(m => m.Horarios)
            .Include(m => m.Prerrequisitos).ThenInclude(p => p.MateriaRequisito)
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.Id)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public Task<Materia?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Materias
            .Include(m => m.Horarios)
            .Include(m => m.Prerrequisitos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Materia>> GetAllAsync(int? carreraId, int? semestre, CancellationToken ct = default)
    {
        var query = context.Materias.AsNoTracking().AsQueryable();
        if (carreraId is not null) query = query.Where(m => m.CarreraId == carreraId);
        if (semestre is not null) query = query.Where(m => m.Semestre == semestre);

        return await query
            .Include(m => m.Horarios)
            .Include(m => m.Prerrequisitos)
            .OrderBy(m => m.Semestre).ThenBy(m => m.Codigo)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Materia>> GetByCarreraAsync(int carreraId, CancellationToken ct = default) =>
        await context.Materias.AsNoTracking()
            .Include(m => m.Horarios)
            .Include(m => m.Prerrequisitos)
            .Where(m => m.CarreraId == carreraId)
            .OrderBy(m => m.Semestre).ThenBy(m => m.Codigo)
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, int>> GetIdsByCodigosAsync(
        IReadOnlyCollection<string> codigos, CancellationToken ct = default)
    {
        var wanted = codigos.ToArray();
        var rows = await context.Materias.AsNoTracking()
            .Where(m => wanted.Contains(m.Codigo))
            .Select(m => new { m.Codigo, m.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Codigo, r => r.Id, StringComparer.Ordinal);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        context.Materias.AnyAsync(m => m.Id == id, ct);

    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct = default) =>
        context.Materias.AnyAsync(m => m.Codigo == codigo && (excludeId == null || m.Id != excludeId), ct);

    public void Add(Materia materia) => context.Materias.Add(materia);

    public void Remove(Materia materia) => context.Materias.Remove(materia);
}
