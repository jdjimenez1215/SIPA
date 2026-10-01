namespace MsInscripcion.Domain.Exceptions;

/// <summary>Domain invariant violation carrying a stable error code (mapped to HTTP 422 by the API).</summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
