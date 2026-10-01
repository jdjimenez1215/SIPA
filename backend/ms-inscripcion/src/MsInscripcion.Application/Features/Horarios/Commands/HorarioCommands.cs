using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Horarios.Dtos;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Exceptions;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.Application.Features.Horarios.Commands;

public sealed record AddHorarioCommand(int MateriaId, DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin)
    : IRequest<HorarioDto>;

public sealed record UpdateHorarioCommand(
    int MateriaId, int HorarioId, DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin)
    : IRequest<HorarioDto>;

public sealed record DeleteHorarioCommand(int MateriaId, int HorarioId) : IRequest;

public sealed record GetHorariosByMateriaQuery(int MateriaId) : IRequest<IReadOnlyList<HorarioDto>>;

public sealed class AddHorarioCommandValidator : AbstractValidator<AddHorarioCommand>
{
    public AddHorarioCommandValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.DiaSemana).IsInEnum().WithMessage("El día de la semana no es válido.");
    }
}

public sealed class UpdateHorarioCommandValidator : AbstractValidator<UpdateHorarioCommand>
{
    public UpdateHorarioCommandValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.HorarioId).GreaterThan(0).WithMessage("El identificador del horario debe ser mayor a 0.");
        RuleFor(x => x.DiaSemana).IsInEnum().WithMessage("El día de la semana no es válido.");
    }
}

public sealed class DeleteHorarioCommandValidator : AbstractValidator<DeleteHorarioCommand>
{
    public DeleteHorarioCommandValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.HorarioId).GreaterThan(0).WithMessage("El identificador del horario debe ser mayor a 0.");
    }
}

public sealed class GetHorariosByMateriaQueryValidator : AbstractValidator<GetHorariosByMateriaQuery>
{
    public GetHorariosByMateriaQueryValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
    }
}

internal static class HorarioChecks
{
    /// <summary>Range must be inverted-free (HoraInicio &lt; HoraFin) and not overlap the other blocks of the materia.</summary>
    public static void EnsureValid(HorarioMateria block, IEnumerable<HorarioMateria> otherBlocksOfMateria)
    {
        if (block.HoraInicio >= block.HoraFin)
            throw new DomainException(
                ErrorCodes.InvalidSchedule,
                "El horario es inválido: la hora de inicio debe ser menor a la hora de fin.");

        var clash = otherBlocksOfMateria.FirstOrDefault(o => o.Id != block.Id && block.OverlapsWith(o));
        if (clash is not null)
            throw new DomainException(
                ErrorCodes.InvalidSchedule,
                $"El horario se cruza con otro bloque de la misma materia ({clash.DiaSemana} {clash.HoraInicio:HH\\:mm}-{clash.HoraFin:HH\\:mm}).");
    }
}

public sealed class AddHorarioCommandHandler(
    IMateriaRepository materias, IHorarioRepository horarios, IUnitOfWork unitOfWork)
    : IRequestHandler<AddHorarioCommand, HorarioDto>
{
    public async Task<HorarioDto> Handle(AddHorarioCommand request, CancellationToken ct)
    {
        if (!await materias.ExistsAsync(request.MateriaId, ct))
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.MateriaId}.");

        var block = new HorarioMateria
        {
            MateriaId = request.MateriaId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin
        };

        HorarioChecks.EnsureValid(block, await horarios.GetByMateriaAsync(request.MateriaId, ct));

        horarios.Add(block);
        await unitOfWork.SaveChangesAsync(ct);
        return block.ToDto();
    }
}

public sealed class UpdateHorarioCommandHandler(IHorarioRepository horarios, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateHorarioCommand, HorarioDto>
{
    public async Task<HorarioDto> Handle(UpdateHorarioCommand request, CancellationToken ct)
    {
        var block = await horarios.GetByIdAsync(request.HorarioId, ct);
        if (block is null || block.MateriaId != request.MateriaId)
            throw new NotFoundException(
                AppErrorCodes.HorarioNotFound,
                $"No existe el horario con id {request.HorarioId} para la materia {request.MateriaId}.");

        // Validate on a detached copy so a rejected change never dirties the tracked entity.
        var candidate = new HorarioMateria
        {
            Id = block.Id,
            MateriaId = block.MateriaId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin
        };
        HorarioChecks.EnsureValid(candidate, await horarios.GetByMateriaAsync(request.MateriaId, ct));

        block.DiaSemana = request.DiaSemana;
        block.HoraInicio = request.HoraInicio;
        block.HoraFin = request.HoraFin;

        await unitOfWork.SaveChangesAsync(ct);
        return block.ToDto();
    }
}

public sealed class DeleteHorarioCommandHandler(IHorarioRepository horarios, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteHorarioCommand>
{
    public async Task Handle(DeleteHorarioCommand request, CancellationToken ct)
    {
        var block = await horarios.GetByIdAsync(request.HorarioId, ct);
        if (block is null || block.MateriaId != request.MateriaId)
            throw new NotFoundException(
                AppErrorCodes.HorarioNotFound,
                $"No existe el horario con id {request.HorarioId} para la materia {request.MateriaId}.");

        horarios.Remove(block);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class GetHorariosByMateriaQueryHandler(IMateriaRepository materias, IHorarioRepository horarios)
    : IRequestHandler<GetHorariosByMateriaQuery, IReadOnlyList<HorarioDto>>
{
    public async Task<IReadOnlyList<HorarioDto>> Handle(GetHorariosByMateriaQuery request, CancellationToken ct)
    {
        if (!await materias.ExistsAsync(request.MateriaId, ct))
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.MateriaId}.");

        return (await horarios.GetByMateriaAsync(request.MateriaId, ct)).Select(h => h.ToDto()).ToList();
    }
}
