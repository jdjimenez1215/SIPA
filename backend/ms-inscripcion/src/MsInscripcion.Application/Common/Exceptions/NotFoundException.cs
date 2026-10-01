namespace MsInscripcion.Application.Common.Exceptions;

/// <summary>Requested resource does not exist (mapped to HTTP 404).</summary>
public class NotFoundException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
