using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class ScheduleOverlapRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        // Active enrollments of the period + earlier candidates of the same request.
        // A materia with the same id as the candidate is skipped: that case is reported only by NoDuplicateRule.
        var others = ctx.ActiveEnrolledMaterias
            .Concat(ctx.CandidatesBefore(candidate))
            .Where(o => o.Id != candidate.Id)
            .DistinctBy(o => o.Id);

        foreach (var other in others)
        {
            var clashes = candidate.Horarios.Any(a => other.Horarios.Any(b => a.OverlapsWith(b)));
            if (!clashes)
                continue;

            yield return new RuleViolation(
                ErrorCodes.ScheduleOverlap,
                candidate.Id,
                candidate.Codigo,
                $"La materia {candidate.Codigo} tiene cruce de horario con {other.Codigo}.",
                new Dictionary<string, object>
                {
                    ["conflictsWith"] = other.Codigo,
                    ["conflictsWithMateriaId"] = other.Id
                });
        }
    }
}
