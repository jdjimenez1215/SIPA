using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Materias.Dtos;
using MsInscripcion.Application.Features.Sugerencia;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.Application.Features.Materias.Queries;

/// <param name="Periodo">Optional; defaults to Enrollment:CurrentPeriod.</param>
public sealed record MateriasDisponiblesQuery(int EstudianteId, string? Periodo = null)
    : IRequest<IReadOnlyList<MateriaDto>>;

public sealed class MateriasDisponiblesQueryValidator : AbstractValidator<MateriasDisponiblesQuery>
{
    public MateriasDisponiblesQueryValidator()
    {
        RuleFor(x => x.EstudianteId).GreaterThan(0)
            .WithMessage("El identificador del estudiante debe ser mayor a 0.");

        RuleFor(x => x.Periodo!)
            .Matches(EnrollmentOptions.PeriodPattern)
            .WithMessage("El periodo debe tener el formato AAAA-1 o AAAA-2 (por ejemplo 2026-2).")
            .When(x => x.Periodo is not null);
    }
}

/// <summary>
/// Delegates to the suggestion calculator (same rules as the enrollment, no locks, advisory only)
/// and returns only the <see cref="EstadoSugerencia.Sugerida"/> rows.
/// </summary>
public sealed class MateriasDisponiblesQueryHandler(
    IStudentRepository students,
    IMateriaRepository materias,
    IInscripcionRepository inscripciones,
    EnrollmentSuggestionCalculator calculator,
    IOptions<EnrollmentOptions> options)
    : IRequestHandler<MateriasDisponiblesQuery, IReadOnlyList<MateriaDto>>
{
    public async Task<IReadOnlyList<MateriaDto>> Handle(MateriasDisponiblesQuery request, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(request.EstudianteId, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.StudentNotFound, $"No existe el estudiante con id {request.EstudianteId}.");

        var period = request.Periodo ?? options.Value.CurrentPeriod;

        var rows = await SuggestionLoader.LoadAsync(
            student, period, options.Value.MaxNextSemesterSubjects,
            students, materias, inscripciones, calculator, ct);

        return rows
            .Where(r => r.Estado == EstadoSugerencia.Sugerida)
            .Select(r => r.Materia.ToDto())
            .ToList();
    }
}
