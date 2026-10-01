using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class HorarioRepository(InscripcionDbContext context) : IHorarioRepository
{
    public Task<HorarioMateria?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Horarios.FirstOrDefaultAsync(h => h.Id == id, ct);

    public async Task<IReadOnlyList<HorarioMateria>> GetByMateriaAsync(int materiaId, CancellationToken ct = default) =>
        // DiaSemana is stored as a string: order by enum value in memory, not alphabetically in SQL.
        (await context.Horarios.AsNoTracking()
            .Where(h => h.MateriaId == materiaId)
            .ToListAsync(ct))
        .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
        .ToList();

    public void Add(HorarioMateria horario) => context.Horarios.Add(horario);

    public void Remove(HorarioMateria horario) => context.Horarios.Remove(horario);
}
