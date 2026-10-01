using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Application.Features.Inscripciones.Commands;

public sealed record CancelCommand(int InscripcionId) : IRequest;

public sealed class CancelCommandValidator : AbstractValidator<CancelCommand>
{
    public CancelCommandValidator()
    {
        RuleFor(x => x.InscripcionId).GreaterThan(0)
            .WithMessage("El identificador de la inscripción debe ser mayor a 0.");
    }
}

/// <summary>Soft cancel: Estado = Cancelada. The seat is freed because seats are counted from Activa rows.</summary>
public sealed class CancelCommandHandler(IInscripcionRepository inscripciones, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelCommand>
{
    public async Task Handle(CancelCommand request, CancellationToken ct)
    {
        var inscripcion = await inscripciones.GetByIdAsync(request.InscripcionId, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.EnrollmentNotFound, $"No existe la inscripción con id {request.InscripcionId}.");

        if (inscripcion.Estado == EstadoInscripcion.Cancelada)
            throw new ConflictException(
                AppErrorCodes.AlreadyCancelled, "La inscripción ya se encuentra cancelada.");

        inscripcion.Estado = EstadoInscripcion.Cancelada;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
