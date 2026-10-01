using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Abstractions.Persistence;

public interface IInscripcionRepository
{
    /// <summary>Tracked inscripcion or null.</summary>
    Task<Inscripcion?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Count of Activa enrollments per materiaId for the period. Missing ids may be absent from the result.</summary>
    Task<IReadOnlyDictionary<int, int>> CountActiveAsync(
        IReadOnlyCollection<int> materiaIds, string period, CancellationToken ct = default);

    /// <summary>Student's Activa enrollments for the period, with Materia and Materia.Horarios loaded.</summary>
    Task<IReadOnlyList<Inscripcion>> GetActiveWithScheduleAsync(
        int studentId, string period, CancellationToken ct = default);

    void AddRange(IEnumerable<Inscripcion> inscripciones);
}
