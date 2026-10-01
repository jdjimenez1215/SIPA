using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Materias.Dtos;

namespace MsInscripcion.Application.Features.Materias.Queries;

/// <summary>Catalog query; both filters are optional.</summary>
public sealed record GetMateriasQuery(int? CarreraId = null, int? Semestre = null)
    : IRequest<IReadOnlyList<MateriaDto>>;

public sealed record GetMateriaByIdQuery(int Id) : IRequest<MateriaDto>;

public sealed class GetMateriasQueryValidator : AbstractValidator<GetMateriasQuery>
{
    public GetMateriasQueryValidator()
    {
        RuleFor(x => x.CarreraId).GreaterThan(0)
            .WithMessage("El identificador de la carrera debe ser mayor a 0.")
            .When(x => x.CarreraId.HasValue);
        RuleFor(x => x.Semestre).GreaterThan(0)
            .WithMessage("El semestre debe ser mayor a 0.")
            .When(x => x.Semestre.HasValue);
    }
}

public sealed class GetMateriaByIdQueryValidator : AbstractValidator<GetMateriaByIdQuery>
{
    public GetMateriaByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
    }
}

public sealed class GetMateriasQueryHandler(IMateriaRepository materias)
    : IRequestHandler<GetMateriasQuery, IReadOnlyList<MateriaDto>>
{
    public async Task<IReadOnlyList<MateriaDto>> Handle(GetMateriasQuery request, CancellationToken ct) =>
        (await materias.GetAllAsync(request.CarreraId, request.Semestre, ct)).Select(m => m.ToDto()).ToList();
}

public sealed class GetMateriaByIdQueryHandler(IMateriaRepository materias)
    : IRequestHandler<GetMateriaByIdQuery, MateriaDto>
{
    public async Task<MateriaDto> Handle(GetMateriaByIdQuery request, CancellationToken ct)
    {
        var materia = await materias.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.Id}.");
        return materia.ToDto();
    }
}
