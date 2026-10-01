using MsInscripcion.Domain.Rules;

namespace MsInscripcion.Application.Common.Exceptions;

/// <summary>Enrollment rejected by the rules engine; carries ALL violations (mapped to HTTP 422).</summary>
public class BusinessRuleViolationException(IReadOnlyList<RuleViolation> violations)
    : Exception("La inscripción fue rechazada por incumplir una o más reglas de negocio.")
{
    public string Code => AppErrorCodes.EnrollmentRejected;

    public IReadOnlyList<RuleViolation> Violations { get; } = violations;
}
