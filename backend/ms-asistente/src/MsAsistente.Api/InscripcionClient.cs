using System.Net.Http.Json;

namespace MsAsistente.Api;

public sealed class InscripcionClient(HttpClient http, IConfiguration configuration)
{
    private readonly string _baseUrl = (configuration["Inscripcion:BaseUrl"] ?? "http://localhost:8080").TrimEnd('/');

    /// <summary>Calls ms-inscripcion with the student's own bearer token (no service credentials).</summary>
    public async Task<SugerenciaDto> ObtenerSugerenciaAsync(
        int estudianteId, string? periodo, string bearerToken, CancellationToken ct)
    {
        var query = string.IsNullOrWhiteSpace(periodo) ? string.Empty : $"?periodo={Uri.EscapeDataString(periodo)}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{_baseUrl}/api/estudiantes/{estudianteId}/sugerencia{query}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SugerenciaDto>(JsonDefaults.Options, ct)
               ?? throw new InvalidOperationException("ms-inscripcion returned an empty suggestion.");
    }
}
