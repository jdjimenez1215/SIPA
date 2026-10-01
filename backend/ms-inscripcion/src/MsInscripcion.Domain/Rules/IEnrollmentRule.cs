using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

/// <summary>One business rule = one Strategy. Pure: no I/O, all data comes through the context.</summary>
public interface IEnrollmentRule
{
    IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate);
}
