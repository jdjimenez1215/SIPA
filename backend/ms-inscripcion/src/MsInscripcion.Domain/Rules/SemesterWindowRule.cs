using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

/// <summary>
/// Candidate window: owed subjects (semestre &lt; N), all of N and N+1. Anything beyond N+1 is rejected.
/// </summary>
public sealed class SemesterWindowRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        var maxAllowed = ctx.Student.SemestreActual + 1;
        if (candidate.Semestre <= maxAllowed)
            yield break;

        yield return new RuleViolation(
            ErrorCodes.SemesterExceeded,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} es del semestre {candidate.Semestre}, y el máximo permitido para el estudiante es {maxAllowed}.",
            new Dictionary<string, object>
            {
                ["materiaSemestre"] = candidate.Semestre,
                ["maxAllowedSemestre"] = maxAllowed
            });
    }
}
