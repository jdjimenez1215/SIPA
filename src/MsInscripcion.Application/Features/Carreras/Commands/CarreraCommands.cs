using FluentValidation;
using MediatR;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Carreras.Dtos;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Features.Carreras.Commands;

public sealed record CreateCarreraCommand(string Codigo, string Nombre, int DuracionSemestres) : IRequest<CarreraDto>;

public sealed record UpdateCarreraCommand(int Id, string Codigo, string Nombre, int DuracionSemestres)
    : IRequest<CarreraDto>;

public sealed record DeleteCarreraCommand(int Id) : IRequest;

internal static class CarreraRules
{
    public static IRuleBuilderOptions<T, string> Codigo<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("El código es obligatorio.")
        .MaximumLength(20).WithMessage("El código no puede superar 20 caracteres.");

    public static IRuleBuilderOptions<T, string> Nombre<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("El nombre es obligatorio.")
        .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

    public static IRuleBuilderOptions<T, int> Duracion<T>(this IRuleBuilder<T, int> rule) => rule
        .GreaterThan(0).WithMessage("La duración en semestres debe ser mayor a 0.");
}

public sealed class CreateCarreraCommandValidator : AbstractValidator<CreateCarreraCommand>
{
    public CreateCarreraCommandValidator()
    {
        RuleFor(x => x.Codigo).Codigo();
        RuleFor(x => x.Nombre).Nombre();
        RuleFor(x => x.DuracionSemestres).Duracion();
    }
}

public sealed class UpdateCarreraCommandValidator : AbstractValidator<UpdateCarreraCommand>
{
    public UpdateCarreraCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la carrera debe ser mayor a 0.");
        RuleFor(x => x.Codigo).Codigo();
        RuleFor(x => x.Nombre).Nombre();
        RuleFor(x => x.DuracionSemestres).Duracion();
    }
}

public sealed class DeleteCarreraCommandValidator : AbstractValidator<DeleteCarreraCommand>
{
    public DeleteCarreraCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la carrera debe ser mayor a 0.");
    }
}

public sealed class CreateCarreraCommandHandler(ICarreraRepository carreras, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCarreraCommand, CarreraDto>
{
    public async Task<CarreraDto> Handle(CreateCarreraCommand request, CancellationToken ct)
    {
        var codigo = request.Codigo.Trim();
        if (await carreras.ExistsByCodigoAsync(codigo, null, ct))
            throw new ConflictException(AppErrorCodes.DuplicateCode, $"Ya existe una carrera con el código {codigo}.");

        var carrera = new Carrera
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            DuracionSemestres = request.DuracionSemestres
        };

        carreras.Add(carrera);
        await unitOfWork.SaveChangesAsync(ct);
        return carrera.ToDto();
    }
}

public sealed class UpdateCarreraCommandHandler(ICarreraRepository carreras, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCarreraCommand, CarreraDto>
{
    public async Task<CarreraDto> Handle(UpdateCarreraCommand request, CancellationToken ct)
    {
        var carrera = await carreras.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.CarreraNotFound, $"No existe la carrera con id {request.Id}.");

        var codigo = request.Codigo.Trim();
        if (await carreras.ExistsByCodigoAsync(codigo, request.Id, ct))
            throw new ConflictException(AppErrorCodes.DuplicateCode, $"Ya existe una carrera con el código {codigo}.");

        carrera.Codigo = codigo;
        carrera.Nombre = request.Nombre.Trim();
        carrera.DuracionSemestres = request.DuracionSemestres;

        await unitOfWork.SaveChangesAsync(ct);
        return carrera.ToDto();
    }
}

public sealed class DeleteCarreraCommandHandler(ICarreraRepository carreras, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCarreraCommand>
{
    public async Task Handle(DeleteCarreraCommand request, CancellationToken ct)
    {
        var carrera = await carreras.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(
                AppErrorCodes.CarreraNotFound, $"No existe la carrera con id {request.Id}.");

        carreras.Remove(carrera);
        // FK violation (23503) is translated to ConflictException(ENTIDAD_EN_USO) by IUnitOfWork.
        await unitOfWork.SaveChangesAsync(ct);
    }
}
