using Microsoft.EntityFrameworkCore;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Infrastructure.Persistence.Repositories;

internal sealed class CarreraRepository(InscripcionDbContext context) : ICarreraRepository
{
    public Task<Carrera?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Carreras.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Carrera>> GetAllAsync(CancellationToken ct = default) =>
        await context.Carreras.AsNoTracking().OrderBy(c => c.Codigo).ToListAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        context.Carreras.AnyAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct = default) =>
        context.Carreras.AnyAsync(c => c.Codigo == codigo && (excludeId == null || c.Id != excludeId), ct);

    public void Add(Carrera carrera) => context.Carreras.Add(carrera);

    public void Remove(Carrera carrera) => context.Carreras.Remove(carrera);
}
