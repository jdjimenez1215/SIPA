using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class SemesterLimitRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        var maxAllowed = ctx.Student.SemestreActual + ctx.MaxSemesterAhead;
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
