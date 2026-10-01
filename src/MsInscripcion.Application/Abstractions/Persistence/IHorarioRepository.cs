using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IHorarioRepository
{
    /// <summary>Tracked horario block or null.</summary>
    Task<HorarioMateria?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<HorarioMateria>> GetByMateriaAsync(int materiaId, CancellationToken ct = default);

    void Add(HorarioMateria horario);
    void Remove(HorarioMateria horario);
}
