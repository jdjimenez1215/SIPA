using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IPrerrequisitoRepository
{
    /// <summary>All prerequisite edges (used to build the PrerequisiteGraph).</summary>
    Task<IReadOnlyList<Prerrequisito>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Prerrequisito>> GetByMateriaAsync(int materiaId, CancellationToken ct = default);

    /// <summary>Tracked edge (materiaId requires requisitoId) or null.</summary>
    Task<Prerrequisito?> FindAsync(int materiaId, int materiaRequisitoId, CancellationToken ct = default);

    void Add(Prerrequisito prerrequisito);
    void Remove(Prerrequisito prerrequisito);
}
