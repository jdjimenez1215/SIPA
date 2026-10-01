using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

/// <summary>
/// Shared cap: owed subjects + N+1 subjects (already active in the period + earlier candidates + the evaluated one)
/// cannot exceed <see cref="EnrollmentContext.MaxExtraSubjects"/>. Subjects of the current semester N do not count.
/// The external contract keeps the name LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO.
/// </summary>
public sealed class ExtraSubjectsCapRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        if (!ctx.IsExtra(candidate))
            yield break;

        var used = ctx.ActiveEnrolledMaterias.Count(m => m.Id != candidate.Id && ctx.IsExtra(m))
                   + ctx.CandidatesBefore(candidate).Count(ctx.IsExtra);

        if (used + 1 <= ctx.MaxExtraSubjects)
            yield break;

        yield return new RuleViolation(
            ErrorCodes.ExtraSubjectsCapExceeded,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} excede el máximo de {ctx.MaxExtraSubjects} materias adeudadas o del semestre siguiente.",
            new Dictionary<string, object>
            {
                ["maxExtraSubjects"] = ctx.MaxExtraSubjects,
                ["used"] = used
            });
    }
}
