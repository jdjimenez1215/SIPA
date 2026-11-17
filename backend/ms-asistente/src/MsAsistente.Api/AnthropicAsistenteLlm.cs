using System.Net.Http.Json;
using System.Text.Json;

namespace MsAsistente.Api;

/// <summary>Anthropic Messages API client. If no API key is configured, the service uses the rules fallback.</summary>
public sealed class AnthropicAsistenteLlm(HttpClient http, IConfiguration configuration, ILogger<AnthropicAsistenteLlm> logger)
    : IAsistenteLlm
{
    public async Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default)
    {
        var apiKey = configuration["ANTHROPIC_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured.");

        var model = configuration["Anthropic:Model"]
                    ?? Environment.GetEnvironmentVariable("ASISTENTE_MODEL")
                    ?? "claude-3-5-haiku-latest";

        var system = """
            Sos un asistente de matrícula de Ingeniería de Sistemas.
            Solo podés PROPONER materias: nunca inscribas ni afirmes que la inscripción está hecha.
            Recibís la sugerencia calculada por el backend. Respetá estrictamente:
            - Solo elegí materias con estado "Sugerida".
            - No superes limiteCreditos.
            - Priorizá las materias de semestres más antiguos (ya vienen ordenadas).
            - No propongas materias bloqueadas.
            Respondé SOLO JSON con la forma {"mensaje":"texto breve en español","materiaIds":[1,2]}.
            """;

        var user = JsonSerializer.Serialize(new
        {
            contexto.NombreEstudiante,
            contexto.SemestreActual,
            contexto.Promedio,
            contexto.LimiteCreditos,
            contexto.MensajeEstudiante,
            sugeridas = contexto.Sugeridas.Select(m => new
            {
                m.Id, m.Codigo, m.Nombre, m.Creditos, m.Semestre, m.CuposDisponibles
            }),
            bloqueadas = contexto.Bloqueadas.Select(m => new
            {
                m.Codigo, m.Motivo, m.PrerrequisitoCumplido
            })
        }, JsonDefaults.Options);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(new
        {
            model,
            max_tokens = configuration.GetValue("Anthropic:MaxTokens", 800),
            system,
            messages = new[] { new { role = "user", content = user } }
        });

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var text = json.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Anthropic returned an empty response.");

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Anthropic response is not JSON.");

        var parsed = JsonSerializer.Deserialize<LlmPropuesta>(text[start..(end + 1)], JsonDefaults.Options)
                     ?? throw new InvalidOperationException("Anthropic response could not be parsed.");

        logger.LogInformation("Propuesta de IA con {Count} materias", parsed.MateriaIds.Count);
        return parsed;
    }
}
