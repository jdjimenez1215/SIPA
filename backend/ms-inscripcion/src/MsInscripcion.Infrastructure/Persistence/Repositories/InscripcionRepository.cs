using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class InscripcionRepository(InscripcionDbContext context) : IInscripcionRepository
{
    public Task<Inscripcion?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Inscripciones.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyDictionary<int, int>> CountActiveAsync(
        IReadOnlyCollection<int> materiaIds, string period, CancellationToken ct = default)
    {
        var ids = materiaIds.ToArray();
        var rows = await context.Inscripciones.AsNoTracking()
            .Where(i => ids.Contains(i.MateriaId)
                        && i.PeriodoAcademico == period
                        && i.Estado == EstadoInscripcion.Activa)
            .GroupBy(i => i.MateriaId)
            .Select(g => new { MateriaId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.MateriaId, r => r.Count);
    }

    public async Task<IReadOnlyList<Inscripcion>> GetActiveWithScheduleAsync(
        int studentId, string period, CancellationToken ct = default) =>
        await context.Inscripciones
            .Include(i => i.Materia).ThenInclude(m => m!.Horarios)
            .Where(i => i.EstudianteId == studentId
                        && i.PeriodoAcademico == period
                        && i.Estado == EstadoInscripcion.Activa)
            .OrderBy(i => i.Id)
            .AsSplitQuery()
            .ToListAsync(ct);

    public void AddRange(IEnumerable<Inscripcion> inscripciones) => context.Inscripciones.AddRange(inscripciones);
}
