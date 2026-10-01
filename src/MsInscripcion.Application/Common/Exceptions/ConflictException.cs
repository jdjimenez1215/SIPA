namespace MsInscripcion.Application.Common.Exceptions;

/// <summary>State conflict: duplicated codigo, entity in use, already cancelled, concurrency (mapped to HTTP 409).</summary>
public class ConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
