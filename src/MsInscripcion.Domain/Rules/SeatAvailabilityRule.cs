using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class SeatAvailabilityRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        var active = ctx.ActiveSeatCounts.GetValueOrDefault(candidate.Id);
        if (active < candidate.CuposMaximos)
            yield break;

        yield return new RuleViolation(
            ErrorCodes.SeatsExhausted,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} no tiene cupos disponibles.",
            new Dictionary<string, object>
            {
                ["cuposMaximos"] = candidate.CuposMaximos,
                ["inscritos"] = active
            });
    }
}
