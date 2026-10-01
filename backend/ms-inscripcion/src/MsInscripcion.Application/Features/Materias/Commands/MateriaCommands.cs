using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Materias.Dtos;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Features.Materias.Commands;

public sealed record CreateMateriaCommand(
    string Codigo, string Nombre, int Creditos, int CarreraId, int Semestre, int CuposMaximos)
    : IRequest<MateriaDto>;

public sealed record UpdateMateriaCommand(
    int Id, string Codigo, string Nombre, int Creditos, int CarreraId, int Semestre, int CuposMaximos)
    : IRequest<MateriaDto>;

public sealed record DeleteMateriaCommand(int Id) : IRequest;

public sealed class CreateMateriaCommandValidator : AbstractValidator<CreateMateriaCommand>
{
    public CreateMateriaCommandValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(20).WithMessage("El código no puede superar 20 caracteres.");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");
        RuleFor(x => x.Creditos).GreaterThan(0).WithMessage("Los créditos deben ser mayores a 0.");
        RuleFor(x => x.CarreraId).GreaterThan(0).WithMessage("El identificador de la carrera debe ser mayor a 0.");
        RuleFor(x => x.Semestre).GreaterThan(0).WithMessage("El semestre debe ser mayor a 0.");
        RuleFor(x => x.CuposMaximos).GreaterThan(0).WithMessage("Los cupos máximos deben ser mayores a 0.");
    }
}

public sealed class UpdateMateriaCommandValidator : AbstractValidator<UpdateMateriaCommand>
{
    public UpdateMateriaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(20).WithMessage("El código no puede superar 20 caracteres.");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");
        RuleFor(x => x.Creditos).GreaterThan(0).WithMessage("Los créditos deben ser mayores a 0.");
        RuleFor(x => x.CarreraId).GreaterThan(0).WithMessage("El identificador de la carrera debe ser mayor a 0.");
        RuleFor(x => x.Semestre).GreaterThan(0).WithMessage("El semestre debe ser mayor a 0.");
        RuleFor(x => x.CuposMaximos).GreaterThan(0).WithMessage("Los cupos máximos deben ser mayores a 0.");
    }
}

public sealed class DeleteMateriaCommandValidator : AbstractValidator<DeleteMateriaCommand>
{
    public DeleteMateriaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la materia debe ser mayor a 0.");
    }
}

public sealed class CreateMateriaCommandHandler(
    IMateriaRepository materias, ICarreraRepository carreras, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMateriaCommand, MateriaDto>
{
    public async Task<MateriaDto> Handle(CreateMateriaCommand request, CancellationToken ct)
    {
        if (!await carreras.ExistsAsync(request.CarreraId, ct))
            throw new NotFoundException(
                AppErrorCodes.CarreraNotFound, $"No existe la carrera con id {request.CarreraId}.");

        var codigo = request.Codigo.Trim();
        if (await materias.ExistsByCodigoAsync(codigo, null, ct))
            throw new ConflictException(AppErrorCodes.DuplicateCode, $"Ya existe una materia con el código {codigo}.");

        var materia = new Materia
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            Creditos = request.Creditos,
            CarreraId = request.CarreraId,
            Semestre = request.Semestre,
            CuposMaximos = request.CuposMaximos
        };

        materias.Add(materia);
        await unitOfWork.SaveChangesAsync(ct);
        return materia.ToDto();
    }
}

public sealed class UpdateMateriaCommandHandler(
    IMateriaRepository materias, ICarreraRepository carreras, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMateriaCommand, MateriaDto>
{
    public async Task<MateriaDto> Handle(UpdateMateriaCommand request, CancellationToken ct)
    {
        var materia = await materias.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.Id}.");

        if (!await carreras.ExistsAsync(request.CarreraId, ct))
            throw new NotFoundException(
                AppErrorCodes.CarreraNotFound, $"No existe la carrera con id {request.CarreraId}.");

        var codigo = request.Codigo.Trim();
        if (await materias.ExistsByCodigoAsync(codigo, request.Id, ct))
            throw new ConflictException(AppErrorCodes.DuplicateCode, $"Ya existe una materia con el código {codigo}.");

        materia.Codigo = codigo;
        materia.Nombre = request.Nombre.Trim();
        materia.Creditos = request.Creditos;
        materia.CarreraId = request.CarreraId;
        materia.Semestre = request.Semestre;
        materia.CuposMaximos = request.CuposMaximos;

        await unitOfWork.SaveChangesAsync(ct);
        return materia.ToDto();
    }
}

public sealed class DeleteMateriaCommandHandler(IMateriaRepository materias, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMateriaCommand>
{
    public async Task Handle(DeleteMateriaCommand request, CancellationToken ct)
    {
        var materia = await materias.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.MateriaNotFound, $"No existe la materia con id {request.Id}.");

        materias.Remove(materia);
        // FK violation (23503) is translated to ConflictException(ENTIDAD_EN_USO) by IUnitOfWork.
        await unitOfWork.SaveChangesAsync(ct);
    }
}
