using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Domain.Exceptions;

namespace MsInscripcion.Api.Middleware;

/// <summary>Single place that turns exceptions into RFC 7807 ProblemDetails (application/problem+json).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public const string InternalErrorCode = "ERROR_INTERNO";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client went away: nothing to write.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var problem = Map(ex);

            if (problem.Status >= 500)
                logger.LogError(ex, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Solicitud rechazada ({Status} {Code}): {Message}",
                    problem.Status, problem.Extensions["code"], ex.Message);

            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        NotFoundException e => Build(StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message, e.Code),

        ConflictException e => Build(StatusCodes.Status409Conflict, "Conflicto", e.Message, e.Code),

        BusinessRuleViolationException e => BuildViolations(e),

        DomainException e => Build(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", e.Message, e.Code),

        ValidationException e => BuildValidation(e),

        _ => Build(
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor",
            "Ocurrió un error inesperado. Si el problema persiste, contacte al administrador indicando el traceId.",
            InternalErrorCode)
    };

    private static ProblemDetails BuildViolations(BusinessRuleViolationException e)
    {
        var problem = Build(
            StatusCodes.Status422UnprocessableEntity,
            "Inscripción rechazada",
            e.Message,
            e.Code);

        problem.Extensions["violations"] = e.Violations.Select(v => new
        {
            code = v.Code,
            materiaId = v.MateriaId,
            materiaCodigo = v.MateriaCodigo,
            message = v.Message,
            details = v.Details
        }).ToList();

        return problem;
    }

    private static ProblemDetails BuildValidation(ValidationException e)
    {
        var duplicated = e.Errors.Any(x => x.ErrorCode == AppErrorCodes.DuplicateMateriaInRequest);
        var code = duplicated ? AppErrorCodes.DuplicateMateriaInRequest : AppErrorCodes.ValidationFailed;

        var problem = Build(
            StatusCodes.Status400BadRequest,
            "Solicitud inválida",
            duplicated
                ? "La solicitud contiene materias repetidas."
                : "Uno o más campos de la solicitud no son válidos.",
            code);

        problem.Extensions["errors"] = e.Errors
            .GroupBy(x => ToCamelCasePath(x.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).Distinct().ToArray());

        return problem;
    }

    private static string ToCamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(segment => JsonNamingPolicy.CamelCase.ConvertName(segment)));

    private static ProblemDetails Build(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["code"] = code;
        return problem;
    }
}
