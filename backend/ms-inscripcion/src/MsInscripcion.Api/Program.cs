using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MsInscripcion.Api.Json;
using MsInscripcion.Api.Middleware;
using MsInscripcion.Application;
using MsInscripcion.Application.Common;
using MsInscripcion.Infrastructure;
using MsInscripcion.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyHHmmConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Malformed bodies / binding errors get the same ProblemDetails contract as every other error.
        options.InvalidModelStateResponseFactory = actionContext =>
        {
            var problem = new ValidationProblemDetails(actionContext.ModelState)
            {
                Type = "https://httpstatuses.io/400",
                Title = "Solicitud inválida",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Uno o más campos de la solicitud no son válidos.",
                Instance = actionContext.HttpContext.Request.Path
            };
            problem.Extensions["code"] = AppErrorCodes.ValidationFailed;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        };
    });

// Framework-generated errors (unmatched route, 405, 415) share the same ProblemDetails contract (code + traceId).
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions.TryAdd("code", ctx.ProblemDetails.Status switch
    {
        StatusCodes.Status401Unauthorized => "NO_AUTENTICADO",
        StatusCodes.Status403Forbidden => "PROHIBIDO",
        StatusCodes.Status404NotFound => "RECURSO_NO_ENCONTRADO",
        StatusCodes.Status405MethodNotAllowed => "METODO_NO_PERMITIDO",
        StatusCodes.Status415UnsupportedMediaType => "TIPO_CONTENIDO_NO_SOPORTADO",
        _ => "ERROR_HTTP"
    });
    ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier);
    ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MsInscripcion API",
        Version = "v1",
        Description = "Microservicio de inscripción de materias."
    });
    options.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Example = new OpenApiString("08:00"),
        Description = "Hora en formato HH:mm"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "accessToken devuelto por POST /api/auth/login de MiniIdentityApi."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---- JWT validation: tokens are issued by MiniIdentityApi (shared issuer/audience/key) ----
// JWT_KEY (env) wins over configuration so both services can share one key in any environment.
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. Set the JWT_KEY environment variable (Jwt__Key).");

if (!builder.Environment.IsDevelopment() && jwtKey.StartsWith("dev-only-", StringComparison.Ordinal))
    throw new InvalidOperationException(
        "Jwt:Key is the development key. Set a real secret through the JWT_KEY environment variable.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MiniIdentityApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MiniIdentityApiUsers";

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<InscripcionDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
