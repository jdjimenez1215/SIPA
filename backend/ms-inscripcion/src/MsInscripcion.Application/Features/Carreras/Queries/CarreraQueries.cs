using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Carreras.Dtos;

namespace MsInscripcion.Application.Features.Carreras.Queries;

public sealed record GetCarrerasQuery : IRequest<IReadOnlyList<CarreraDto>>;

public sealed record GetCarreraByIdQuery(int Id) : IRequest<CarreraDto>;

public sealed class GetCarreraByIdQueryValidator : AbstractValidator<GetCarreraByIdQuery>
{
    public GetCarreraByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la carrera debe ser mayor a 0.");
    }
}

public sealed class GetCarrerasQueryHandler(ICarreraRepository carreras)
    : IRequestHandler<GetCarrerasQuery, IReadOnlyList<CarreraDto>>
{
    public async Task<IReadOnlyList<CarreraDto>> Handle(GetCarrerasQuery request, CancellationToken ct) =>
        (await carreras.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();
}

public sealed class GetCarreraByIdQueryHandler(ICarreraRepository carreras)
    : IRequestHandler<GetCarreraByIdQuery, CarreraDto>
{
    public async Task<CarreraDto> Handle(GetCarreraByIdQuery request, CancellationToken ct)
    {
        var carrera = await carreras.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.CarreraNotFound, $"No existe la carrera con id {request.Id}.");
        return carrera.ToDto();
    }
}
