using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IStudentRepository
{
    Task<Estudiante?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Takes a pessimistic row lock (FOR UPDATE) on the student and returns it; null if it does not exist. Requires an open transaction.</summary>
    Task<Estudiante?> LockByIdAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>Ids of materias with an Aprobada history entry (Cursando/Reprobada do NOT count).</summary>
    Task<IReadOnlySet<int>> GetApprovedMateriaIdsAsync(int studentId, CancellationToken ct = default);
}
