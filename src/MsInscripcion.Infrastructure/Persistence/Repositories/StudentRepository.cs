using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using Npgsql;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository(InscripcionDbContext context) : IStudentRepository
{
    public Task<Estudiante?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Estudiantes.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Estudiante?> LockByIdAsync(int id, CancellationToken ct = default)
    {
        // Step 1: row lock on the student (scalar query, no composition over FOR UPDATE).
        // Lock-time deadlocks are translated like the ones raised by SaveChanges (409 CONFLICTO_CONCURRENCIA).
        try
        {
            await context.Database
                .SqlQuery<int>($"SELECT id AS \"Value\" FROM estudiantes WHERE id = {id} FOR UPDATE")
                .ToListAsync(ct);
        }
        catch (PostgresException pg)
        {
            throw UnitOfWork.Translate(pg, pg);
        }

        // Step 2: regular load of the (now locked) row.
        return await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        context.Estudiantes.AnyAsync(e => e.Id == id, ct);

    public async Task<IReadOnlySet<int>> GetApprovedMateriaIdsAsync(int studentId, CancellationToken ct = default)
    {
        var ids = await context.Historial.AsNoTracking()
            .Where(h => h.EstudianteId == studentId && h.Estado == EstadoHistorial.Aprobada)
            .Select(h => h.MateriaId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }
}
