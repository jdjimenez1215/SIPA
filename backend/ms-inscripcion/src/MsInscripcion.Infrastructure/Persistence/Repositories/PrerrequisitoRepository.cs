using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class PrerrequisitoRepository(InscripcionDbContext context) : IPrerrequisitoRepository
{
    public async Task<IReadOnlyList<Prerrequisito>> GetAllAsync(CancellationToken ct = default) =>
        await context.Prerrequisitos.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Prerrequisito>> GetByMateriaAsync(int materiaId, CancellationToken ct = default) =>
        await context.Prerrequisitos.AsNoTracking()
            .Where(p => p.MateriaId == materiaId)
            .OrderBy(p => p.MateriaRequisitoId)
            .ToListAsync(ct);

    public Task<Prerrequisito?> FindAsync(int materiaId, int materiaRequisitoId, CancellationToken ct = default) =>
        context.Prerrequisitos.FirstOrDefaultAsync(
            p => p.MateriaId == materiaId && p.MateriaRequisitoId == materiaRequisitoId, ct);

    public void Add(Prerrequisito prerrequisito) => context.Prerrequisitos.Add(prerrequisito);

    public void Remove(Prerrequisito prerrequisito) => context.Prerrequisitos.Remove(prerrequisito);
}
