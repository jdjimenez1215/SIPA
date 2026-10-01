using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Materias.Dtos;
using MsInscripcion.Domain.Rules;

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
/// Evaluates each candidate ALONE with the same rules engine used by the enrollment (no locks, advisory only).
/// </summary>
public sealed class MateriasDisponiblesQueryHandler(
    IStudentRepository students,
    IMateriaRepository materias,
    IInscripcionRepository inscripciones,
    EnrollmentRulesEngine engine,
    IOptions<EnrollmentOptions> options)
    : IRequestHandler<MateriasDisponiblesQuery, IReadOnlyList<MateriaDto>>
{
    public async Task<IReadOnlyList<MateriaDto>> Handle(MateriasDisponiblesQuery request, CancellationToken ct)
    {
        var student = await students.GetByIdAsync(request.EstudianteId, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.StudentNotFound, $"No existe el estudiante con id {request.EstudianteId}.");

        var period = request.Periodo ?? options.Value.CurrentPeriod;

        var approved = await students.GetApprovedMateriaIdsAsync(student.Id, ct);
        var careerMaterias = await materias.GetByCarreraAsync(student.CarreraId, ct);
        var candidates = careerMaterias.Where(m => !approved.Contains(m.Id)).ToList();

        if (candidates.Count == 0)
            return [];

        var activeEnrollments = await inscripciones.GetActiveWithScheduleAsync(student.Id, period, ct);
        var seatCounts = await inscripciones.CountActiveAsync(candidates.Select(m => m.Id).ToList(), period, ct);

        var baseContext = new EnrollmentContext(
            student,
            period,
            options.Value.MaxSemestersAhead,
            approved,
            activeEnrollments.Where(i => i.Materia is not null).Select(i => i.Materia!).ToList(),
            seatCounts,
            candidates);

        return candidates
            .Where(m => engine.Evaluate(baseContext with { Candidates = [m] }).Count == 0)
            .Select(m => m.ToDto())
            .ToList();
    }
}
