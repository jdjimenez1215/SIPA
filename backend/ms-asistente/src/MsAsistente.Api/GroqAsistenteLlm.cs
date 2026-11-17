using System.Net.Http.Json;
using System.Text.Json;

namespace MsAsistente.Api;

/// <summary>
/// Groq (OpenAI-compatible Chat Completions). Enabled when GROQ_API_KEY is set.
/// The model is configurable because Groq retires model ids frequently.
/// </summary>
public sealed class GroqAsistenteLlm(HttpClient http, IConfiguration configuration, ILogger<GroqAsistenteLlm> logger)
    : IAsistenteLlm
{
    public async Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default)
    {
        var apiKey = configuration["GROQ_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GROQ_API_KEY is not configured.");

        var model = configuration["Groq:Model"]
                    ?? configuration["ASISTENTE_MODEL"]
                    ?? Environment.GetEnvironmentVariable("ASISTENTE_MODEL")
                    ?? "openai/gpt-oss-20b";

        var system = """
            Sos un asistente de matrícula de Ingeniería de Sistemas.
            Solo podés PROPONER materias: nunca inscribas ni afirmes que la inscripción está hecha.
            Recibís la sugerencia calculada por el backend. Respetá estrictamente:
            - Solo elegí materias con estado "Sugerida".
            - No superes limiteCreditos.
            - Priorizá las materias de semestres más antiguos (ya vienen ordenadas).
            - No propongas materias bloqueadas.
            - Si el estudiante dice que no quiere ciertos días, no propongas materias con horario en esos días.
            Respondé SOLO JSON, sin markdown, con la forma {"mensaje":"texto breve en español","materiaIds":[1,2]}.
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
                m.Id, m.Codigo, m.Nombre, m.Creditos, m.Semestre, m.CuposDisponibles,
                horarios = m.Horarios
            }),
            bloqueadas = contexto.Bloqueadas.Select(m => new
            {
                m.Codigo, m.Motivo, m.PrerrequisitoCumplido
            })
        }, JsonDefaults.Options);

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            max_tokens = configuration.GetValue("Anthropic:MaxTokens", 800),
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        });

        using var response = await http.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Groq error {(int)response.StatusCode}: {raw[..Math.Min(raw.Length, 300)]}");

        using var json = JsonDocument.Parse(raw);
        var text = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Groq returned an empty response.");

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Groq response is not JSON.");

        var parsed = JsonSerializer.Deserialize<LlmPropuesta>(text[start..(end + 1)], JsonDefaults.Options)
                     ?? throw new InvalidOperationException("Groq response could not be parsed.");

        logger.LogInformation("Propuesta de Groq ({Model}) con {Count} materias", model, parsed.MateriaIds.Count);
        return parsed;
    }
}
