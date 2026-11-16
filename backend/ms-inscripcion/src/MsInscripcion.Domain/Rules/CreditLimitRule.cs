using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

/// <summary>
/// Credit cap per period: 18 credits, or 20 when the student's historical average is above 4.0.
/// Counts already-active enrollments plus earlier candidates of the same request.
/// </summary>
public sealed class CreditLimitRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        var used = ctx.ActiveEnrolledMaterias.Sum(m => m.Creditos)
                   + ctx.CandidatesBefore(candidate).Sum(m => m.Creditos);
        var total = used + candidate.Creditos;

        if (total <= ctx.MaxCredits)
            yield break;

        yield return new RuleViolation(
            ErrorCodes.CreditLimitExceeded,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} excede el máximo de {ctx.MaxCredits} créditos del semestre " +
            $"({used} créditos ya seleccionados + {candidate.Creditos}).",
            new Dictionary<string, object>
            {
                ["maxCredits"] = ctx.MaxCredits,
                ["creditsUsed"] = used,
                ["candidateCredits"] = candidate.Creditos,
                ["studentAverage"] = ctx.StudentAverage
            });
    }
}
