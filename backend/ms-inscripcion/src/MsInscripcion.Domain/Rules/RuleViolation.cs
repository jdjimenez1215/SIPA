namespace MsInscripcion.Domain.Rules;

public sealed record RuleViolation(
    string Code,
    int MateriaId,
    string MateriaCodigo,
    string Message,
    IReadOnlyDictionary<string, object>? Details = null);
