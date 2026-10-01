using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

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
