using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Sugerencia.Dtos;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.Application.Features.Sugerencia.Queries;

/// <param name="Periodo">Optional; defaults to Enrollment:CurrentPeriod.</param>
public sealed record GetSugerenciaQuery(int EstudianteId, string? Periodo = null) : IRequest<SugerenciaDto>;

public sealed class GetSugerenciaQueryValidator : AbstractValidator<GetSugerenciaQuery>
{
    public GetSugerenciaQueryValidator()
    {
        RuleFor(x => x.EstudianteId).GreaterThan(0)
            .WithMessage("El identificador del estudiante debe ser mayor a 0.");

        RuleFor(x => x.Periodo!)
            .Matches(EnrollmentOptions.PeriodPattern)
            .WithMessage("El periodo debe tener el formato AAAA-1 o AAAA-2 (por ejemplo 2026-2).")
            .When(x => x.Periodo is not null);
    }
}

public sealed class GetSugerenciaQueryHandler(
    IStudentRepository students,
    ICarreraRepository carreras,
    IMateriaRepository materias,
    IInscripcionRepository inscripciones,
    EnrollmentSuggestionCalculator calculator,
    IOptions<EnrollmentOptions> options)
    : IRequestHandler<GetSugerenciaQuery, SugerenciaDto>
{
    public async Task<SugerenciaDto> Handle(GetSugerenciaQuery request, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(request.EstudianteId, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.StudentNotFound, $"No existe el estudiante con id {request.EstudianteId}.");

        var period = request.Periodo ?? options.Value.CurrentPeriod;
        var carrera = await carreras.GetByIdAsync(student.CarreraId, ct);

        var rows = await SuggestionLoader.LoadAsync(
            student, period, options.Value.MaxNextSemesterSubjects,
            students, materias, inscripciones, calculator, ct);

        var sugeridas = rows
            .Select(r => new MateriaSugeridaDto(
                r.Materia.Id,
                r.Materia.Codigo,
                r.Materia.Nombre,
                r.Materia.Creditos,
                r.Materia.Semestre,
                r.Estado,
                r.PrerrequisitoCumplido,
                r.Motivo))
            .ToList();

        var totalCreditos = sugeridas.Where(s => s.Estado == EstadoSugerencia.Sugerida).Sum(s => s.Creditos);

        return new SugerenciaDto(
            student.Nombre,
            carrera?.Nombre ?? string.Empty,
            student.SemestreActual,
            period,
            totalCreditos,
            sugeridas,
            []);
    }
}
