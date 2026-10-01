using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface ICarreraRepository
{
    /// <summary>Tracked carrera or null.</summary>
    Task<Carrera?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Carrera>> GetAllAsync(CancellationToken ct = default);

    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>True if another carrera (different id) already uses the codigo.</summary>
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct = default);

    void Add(Carrera carrera);
    void Remove(Carrera carrera);
}
