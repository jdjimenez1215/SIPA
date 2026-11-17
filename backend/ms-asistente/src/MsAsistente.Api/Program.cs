using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MsAsistente.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<InscripcionClient>();
builder.Services.AddHttpClient<AnthropicAsistenteLlm>(client =>
{
    var baseUrl = builder.Configuration["Anthropic:BaseUrl"] ?? "https://api.anthropic.com";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<GroqAsistenteLlm>(client =>
{
    var baseUrl = builder.Configuration["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<ReglasAsistenteLlm>();
builder.Services.AddScoped<IAsistenteLlm>(sp =>
{
    var groqKey = builder.Configuration["GROQ_API_KEY"]
                  ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
    if (!string.IsNullOrWhiteSpace(groqKey))
        return sp.GetRequiredService<GroqAsistenteLlm>();

    var anthropicKey = builder.Configuration["ANTHROPIC_API_KEY"]
                       ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    return string.IsNullOrWhiteSpace(anthropicKey)
        ? sp.GetRequiredService<ReglasAsistenteLlm>()
        : sp.GetRequiredService<AnthropicAsistenteLlm>();
});
builder.Services.AddScoped<AsistenteService>();

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("JWT_KEY is missing or shorter than 32 bytes.");

var issuer = builder.Configuration["Jwt:Issuer"] ?? "MiniIdentityApi";
var audience = builder.Configuration["Jwt:Audience"] ?? "MiniIdentityApiUsers";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/ready", () => Results.Ok(new { status = "ready" }));

app.MapPost("/api/asistente/chat", async (
    ChatRequest request,
    HttpContext http,
    AsistenteService service,
    InscripcionClient inscripcion,
    CancellationToken ct) =>
{
    // The student id comes ONLY from the validated JWT: never from the request body.
    var rawId = http.User.FindFirst("estudianteId")?.Value;
    if (!int.TryParse(rawId, out var estudianteId))
    {
        return Results.Problem(
            title: "Asistente solo para estudiantes",
            detail: "La cuenta autenticada no está vinculada a un estudiante.",
            statusCode: StatusCodes.Status403Forbidden,
            extensions: new Dictionary<string, object?> { ["code"] = "ASISTENTE_SOLO_ESTUDIANTES" });
    }

    var authorization = http.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.Unauthorized();
    var token = authorization["Bearer ".Length..].Trim();

    try
    {
        var sugerencia = await inscripcion.ObtenerSugerenciaAsync(estudianteId, request.Periodo, token, ct);
        var respuesta = await service.ResponderAsync(sugerencia, request.Mensaje ?? string.Empty, ct);
        return Results.Ok(respuesta);
    }
    catch (HttpRequestException ex)
    {
        app.Logger.LogWarning(ex, "ms-inscripcion no respondió al asistente");
        return Results.Problem(
            title: "Servicio de inscripción no disponible",
            detail: "No se pudo consultar la sugerencia del estudiante.",
            statusCode: StatusCodes.Status502BadGateway,
            extensions: new Dictionary<string, object?> { ["code"] = "ASISTENTE_SIN_INSCRIPCION" });
    }
}).RequireAuthorization();

app.Run();

public partial class Program;
