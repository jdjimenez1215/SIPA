namespace MsInscripcion.Domain.Rules;

public sealed class EnrollmentRulesEngine(IEnumerable<IEnrollmentRule> rules)
{
    private readonly IReadOnlyList<IEnrollmentRule> _rules = rules.ToList();

    /// <summary>Runs ALL rules for ALL candidates (no short-circuit) and returns every violation.</summary>
    public IReadOnlyList<RuleViolation> Evaluate(EnrollmentContext ctx) =>
        ctx.Candidates
            .SelectMany(candidate => _rules.SelectMany(rule => rule.Evaluate(ctx, candidate)))
            .ToList();
}
