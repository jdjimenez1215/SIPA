using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IMateriaRepository
{
    /// <summary>
    /// Takes pessimistic row locks (SELECT ... FOR UPDATE, ORDER BY id) on the given materias and returns the ones found,
    /// ordered by id, with Horarios and Prerrequisitos loaded. MUST be called inside an open transaction.
    /// </summary>
    Task<IReadOnlyList<Materia>> LockByIdsAsync(IReadOnlyCollection<int> orderedIds, CancellationToken ct = default);

    /// <summary>Tracked materia with Horarios and Prerrequisitos, or null.</summary>
    Task<Materia?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Materias with Horarios and Prerrequisitos, optionally filtered, ordered by semestre then codigo.</summary>
    Task<IReadOnlyList<Materia>> GetAllAsync(int? carreraId, int? semestre, CancellationToken ct = default);

    /// <summary>All materias of a carrera with Horarios and Prerrequisitos.</summary>
    Task<IReadOnlyList<Materia>> GetByCarreraAsync(int carreraId, CancellationToken ct = default);

    /// <summary>Maps each known codigo to its materia id (unknown codes are absent). No locks.</summary>
    Task<IReadOnlyDictionary<string, int>> GetIdsByCodigosAsync(IReadOnlyCollection<string> codigos, CancellationToken ct = default);

    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>True if another materia (different id) already uses the codigo.</summary>
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct = default);

    void Add(Materia materia);
    void Remove(Materia materia);
}
