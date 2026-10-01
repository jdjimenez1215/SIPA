using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed record EnrollmentContext(
    Estudiante Student,
    string Period,
    int MaxExtraSubjects,                                // shared cap: owed + N+1
    IReadOnlySet<int> ApprovedMateriaIds,
    IReadOnlyList<Materia> ActiveEnrolledMaterias,      // same period, with Horarios
    IReadOnlyDictionary<int, int> ActiveSeatCounts,      // materiaId -> active count for the period
    IReadOnlyList<Materia> Candidates)                   // request order, with Horarios + Prerrequisitos
{
    /// <summary>Candidates that appear earlier in the request than the given materia.</summary>
    public IEnumerable<Materia> CandidatesBefore(Materia m) => Candidates.TakeWhile(c => c.Id != m.Id);

    /// <summary>
    /// "Extra" subject: from the student's career, not approved, and either owed (Semestre &lt; N) or of N+1.
    /// </summary>
    public bool IsExtra(Materia m) =>
        m.CarreraId == Student.CarreraId
        && !ApprovedMateriaIds.Contains(m.Id)
        && (m.Semestre < Student.SemestreActual || m.Semestre == Student.SemestreActual + 1);
}
