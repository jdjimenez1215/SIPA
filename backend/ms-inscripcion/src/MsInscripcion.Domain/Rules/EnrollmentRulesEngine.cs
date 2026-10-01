using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class EnrollmentRulesEngine(IEnumerable<IEnrollmentRule> rules)
{
    private readonly IReadOnlyList<IEnrollmentRule> _rules = rules.ToList();

    /// <summary>Runs ALL rules for ALL candidates (no short-circuit) and returns every violation.</summary>
    public IReadOnlyList<RuleViolation> Evaluate(EnrollmentContext ctx) =>
        ctx.Candidates
            .SelectMany(candidate => EvaluateCandidate(ctx, candidate))
            .ToList();

    /// <summary>Runs ALL rules for a single candidate (no short-circuit) and returns its violations.</summary>
    public IReadOnlyList<RuleViolation> EvaluateCandidate(EnrollmentContext ctx, Materia candidate) =>
        _rules.SelectMany(rule => rule.Evaluate(ctx, candidate)).ToList();
}
