using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Prerrequisitos.Dtos;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.Application.Features.Prerrequisitos.Commands;

/// <summary>MateriaId requires MateriaRequisitoId.</summary>
public sealed record AddPrerrequisitoCommand(int MateriaId, int MateriaRequisitoId) : IRequest<PrerrequisitoDto>;

public sealed record DeletePrerrequisitoCommand(int MateriaId, int MateriaRequisitoId) : IRequest;

public sealed record GetPrerrequisitosByMateriaQuery(int MateriaId) : IRequest<IReadOnlyList<PrerrequisitoDto>>;

public sealed class AddPrerrequisitoCommandValidator : AbstractValidator<AddPrerrequisitoCommand>
{
    public AddPrerrequisitoCommandValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.MateriaRequisitoId).GreaterThan(0)
            .WithMessage("El identificador de la materia requisito debe ser mayor a 0.");
    }
}

public sealed class DeletePrerrequisitoCommandValidator : AbstractValidator<DeletePrerrequisitoCommand>
{
    public DeletePrerrequisitoCommandValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.MateriaRequisitoId).GreaterThan(0)
            .WithMessage("El identificador de la materia requisito debe ser mayor a 0.");
    }
}

public sealed class GetPrerrequisitosByMateriaQueryValidator : AbstractValidator<GetPrerrequisitosByMateriaQuery>
{
    public GetPrerrequisitosByMateriaQueryValidator()
    {
        RuleFor(x => x.MateriaId).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
    }
}

public sealed class AddPrerrequisitoCommandHandler(
    IMateriaRepository materias, IPrerrequisitoRepository prerrequisitos, IUnitOfWork unitOfWork)
    : IRequestHandler<AddPrerrequisitoCommand, PrerrequisitoDto>
{
    public async Task<PrerrequisitoDto> Handle(AddPrerrequisitoCommand request, CancellationToken ct)
    {
        if (!await materias.ExistsAsync(request.MateriaId, ct))
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.MateriaId}.");
        if (!await materias.ExistsAsync(request.MateriaRequisitoId, ct))
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.MateriaRequisitoId}.");

        var edges = await prerrequisitos.GetAllAsync(ct);

        // Self-reference and direct/indirect cycles -> DomainException PRERREQUISITO_CICLICO (422).
        new PrerequisiteGraph(edges.Select(e => (e.MateriaId, e.MateriaRequisitoId)))
            .EnsureNoCycle(request.MateriaId, request.MateriaRequisitoId);

        if (edges.Any(e => e.MateriaId == request.MateriaId && e.MateriaRequisitoId == request.MateriaRequisitoId))
            throw new ConflictException(
                AppErrorCodes.DuplicatePrerequisite, "El prerrequisito ya está registrado para la materia.");

        var edge = new Prerrequisito
        {
            MateriaId = request.MateriaId,
            MateriaRequisitoId = request.MateriaRequisitoId
        };

        prerrequisitos.Add(edge);
        await unitOfWork.SaveChangesAsync(ct);
        return edge.ToDto();
    }
}

public sealed class DeletePrerrequisitoCommandHandler(IPrerrequisitoRepository prerrequisitos, IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePrerrequisitoCommand>
{
    public async Task Handle(DeletePrerrequisitoCommand request, CancellationToken ct)
    {
        var edge = await prerrequisitos.FindAsync(request.MateriaId, request.MateriaRequisitoId, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.PrerequisiteNotFound,
                $"La materia {request.MateriaId} no tiene como prerrequisito a la materia {request.MateriaRequisitoId}.");

        prerrequisitos.Remove(edge);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class GetPrerrequisitosByMateriaQueryHandler(
    IMateriaRepository materias, IPrerrequisitoRepository prerrequisitos)
    : IRequestHandler<GetPrerrequisitosByMateriaQuery, IReadOnlyList<PrerrequisitoDto>>
{
    public async Task<IReadOnlyList<PrerrequisitoDto>> Handle(GetPrerrequisitosByMateriaQuery request, CancellationToken ct)
    {
        if (!await materias.ExistsAsync(request.MateriaId, ct))
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.MateriaId}.");

        return (await prerrequisitos.GetByMateriaAsync(request.MateriaId, ct)).Select(p => p.ToDto()).ToList();
    }
}
