using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed record EnrollmentContext(
    Estudiante Student,
    string Period,
    int MaxSemesterAhead,
    IReadOnlySet<int> ApprovedMateriaIds,
    IReadOnlyList<Materia> ActiveEnrolledMaterias,      // same period, with Horarios
    IReadOnlyDictionary<int, int> ActiveSeatCounts,      // materiaId -> active count for the period
    IReadOnlyList<Materia> Candidates)                   // request order, with Horarios + Prerrequisitos
{
    /// <summary>Candidates that appear earlier in the request than the given materia.</summary>
    public IEnumerable<Materia> CandidatesBefore(Materia m) => Candidates.TakeWhile(c => c.Id != m.Id);
}
