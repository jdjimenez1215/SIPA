using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Inscripciones.Dtos;

namespace MsInscripcion.Application.Features.Inscripciones.Queries;

/// <param name="Periodo">Optional; defaults to Enrollment:CurrentPeriod.</param>
public sealed record GetInscripcionesQuery(int EstudianteId, string? Periodo = null)
    : IRequest<IReadOnlyList<InscripcionDto>>;

public sealed class GetInscripcionesQueryValidator : AbstractValidator<GetInscripcionesQuery>
{
    public GetInscripcionesQueryValidator()
    {
        RuleFor(x => x.EstudianteId).GreaterThan(0)
            .WithMessage("El identificador del estudiante debe ser mayor a 0.");

        RuleFor(x => x.Periodo!)
            .Matches(EnrollmentOptions.PeriodPattern)
            .WithMessage("El periodo debe tener el formato AAAA-1 o AAAA-2 (por ejemplo 2026-2).")
            .When(x => x.Periodo is not null);
    }
}

public sealed class GetInscripcionesQueryHandler(
    IStudentRepository students,
    IInscripcionRepository inscripciones,
    IOptions<EnrollmentOptions> options)
    : IRequestHandler<GetInscripcionesQuery, IReadOnlyList<InscripcionDto>>
{
    public async Task<IReadOnlyList<InscripcionDto>> Handle(GetInscripcionesQuery request, CancellationToken ct)
    {
        if (!await students.ExistsAsync(request.EstudianteId, ct))
            throw new NotFoundException(
                AppErrorCodes.StudentNotFound, $"No existe el estudiante con id {request.EstudianteId}.");

        var period = request.Periodo ?? options.Value.CurrentPeriod;
        var active = await inscripciones.GetActiveWithScheduleAsync(request.EstudianteId, period, ct);

        return active.Select(i => i.ToDto()).ToList();
    }
}
