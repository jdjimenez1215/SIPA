using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class CareerMatchRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        if (candidate.CarreraId == ctx.Student.CarreraId)
            yield break;

        yield return new RuleViolation(
            ErrorCodes.CareerMismatch,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} no pertenece a la carrera del estudiante.");
    }
}
