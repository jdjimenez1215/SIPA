using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class NoDuplicateRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        if (ctx.ApprovedMateriaIds.Contains(candidate.Id))
        {
            yield return new RuleViolation(
                ErrorCodes.AlreadyApproved,
                candidate.Id,
                candidate.Codigo,
                $"La materia {candidate.Codigo} ya fue aprobada por el estudiante.");
        }

        if (ctx.ActiveEnrolledMaterias.Any(m => m.Id == candidate.Id))
        {
            yield return new RuleViolation(
                ErrorCodes.AlreadyEnrolled,
                candidate.Id,
                candidate.Codigo,
                $"El estudiante ya está inscrito en la materia {candidate.Codigo} para el periodo {ctx.Period}.");
        }
    }
}
