using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Inscripciones.Dtos;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.Application.Features.Inscripciones.Commands;

/// <summary>Exactly one of <paramref name="MateriaIds"/> or <paramref name="CodigosMaterias"/> must be non-empty.</summary>
public sealed record EnrollCommand(
    int EstudianteId,
    string Periodo,
    IReadOnlyList<int>? MateriaIds,
    IReadOnlyList<string>? CodigosMaterias = null)
    : IRequest<IReadOnlyList<InscripcionDto>>;

public sealed class EnrollCommandValidator : AbstractValidator<EnrollCommand>
{
    public EnrollCommandValidator()
    {
        RuleFor(x => x.EstudianteId)
            .GreaterThan(0).WithMessage("El identificador del estudiante debe ser mayor a 0.");

        RuleFor(x => x.Periodo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El periodo es obligatorio.")
            .Matches(EnrollmentOptions.PeriodPattern)
            .WithMessage("El periodo debe tener el formato AAAA-1 o AAAA-2 (por ejemplo 2026-2).");

        RuleFor(x => x)
            .Must(x => HasIds(x) ^ HasCodes(x))
            .WithName("materias")
            .WithMessage("Debe indicar exactamente uno de 'materiaIds' o 'codigosMaterias' con al menos una materia.");

        RuleFor(x => x.MateriaIds!)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithErrorCode(AppErrorCodes.DuplicateMateriaInRequest)
            .WithMessage("La solicitud contiene materias repetidas.")
            .When(OnlyIds);

        RuleForEach(x => x.MateriaIds!)
            .GreaterThan(0).WithMessage("Los identificadores de materia deben ser mayores a 0.")
            .When(OnlyIds);

        RuleFor(x => x.CodigosMaterias!)
            .Must(codes => codes.Distinct(StringComparer.Ordinal).Count() == codes.Count)
            .WithErrorCode(AppErrorCodes.DuplicateMateriaInRequest)
            .WithMessage("La solicitud contiene materias repetidas.")
            .When(OnlyCodes);

        RuleForEach(x => x.CodigosMaterias!)
            .NotEmpty().WithMessage("Los códigos de materia no pueden estar vacíos.")
            .When(OnlyCodes);
    }

    private static bool HasIds(EnrollCommand x) => x.MateriaIds is { Count: > 0 };

    private static bool HasCodes(EnrollCommand x) => x.CodigosMaterias is { Count: > 0 };

    // List rules only run when exactly one list was sent; otherwise the XOR rule alone reports VALIDACION_FALLIDA.
    private static bool OnlyIds(EnrollCommand x) => HasIds(x) && !HasCodes(x);

    private static bool OnlyCodes(EnrollCommand x) => HasCodes(x) && !HasIds(x);
}

public sealed class EnrollCommandHandler(
    IStudentRepository students,
    IMateriaRepository materias,
    IInscripcionRepository inscripciones,
    IUnitOfWork unitOfWork,
    EnrollmentRulesEngine engine,
    IOptions<EnrollmentOptions> options,
    TimeProvider clock)
    : IRequestHandler<EnrollCommand, IReadOnlyList<InscripcionDto>>
{
    public async Task<IReadOnlyList<InscripcionDto>> Handle(EnrollCommand request, CancellationToken ct)
    {
        // Codes are resolved with a plain read BEFORE the transaction: it does not alter the lock order.
        var requestedIds = await ResolveMateriaIdsAsync(request, ct);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        // Lock order is always student -> materias (ascending id), held until commit/rollback.
        // The student lock serializes concurrent requests of the same student (R3 overlap races between
        // different materias); the fixed order prevents deadlocks.
        var student = await students.LockByIdAsync(request.EstudianteId, ct);
        if (student is null)
        {
            await transaction.RollbackAsync(ct);
            throw new NotFoundException(
                AppErrorCodes.StudentNotFound, $"No existe el estudiante con id {request.EstudianteId}.");
        }

        var lockIds = requestedIds.OrderBy(id => id).ToList();
        var locked = await materias.LockByIdsAsync(lockIds, ct);

        var missing = lockIds.Except(locked.Select(m => m.Id)).ToList();
        if (missing.Count > 0)
        {
            await transaction.RollbackAsync(ct);
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound,
                $"No existe(n) la(s) materia(s) con id: {string.Join(", ", missing)}.");
        }

        var byId = locked.ToDictionary(m => m.Id);
        var candidates = requestedIds.Select(id => byId[id]).ToList(); // request order

        var approved = await students.GetApprovedMateriaIdsAsync(student.Id, ct);
        var activeEnrollments = await inscripciones.GetActiveWithScheduleAsync(student.Id, request.Periodo, ct);
        var seatCounts = await inscripciones.CountActiveAsync(lockIds, request.Periodo, ct);

        var context = new EnrollmentContext(
            student,
            request.Periodo,
            options.Value.MaxNextSemesterSubjects,
            approved,
            activeEnrollments.Where(i => i.Materia is not null).Select(i => i.Materia!).ToList(),
            seatCounts,
            candidates);

        var violations = engine.Evaluate(context);
        if (violations.Count > 0)
        {
            await transaction.RollbackAsync(ct);
            throw new BusinessRuleViolationException(violations);
        }

        var now = clock.GetUtcNow();
        var created = candidates
            .Select(m => new Inscripcion
            {
                EstudianteId = student.Id,
                MateriaId = m.Id,
                PeriodoAcademico = request.Periodo,
                Estado = EstadoInscripcion.Activa,
                FechaInscripcion = now
            })
            .ToList();

        inscripciones.AddRange(created);
        await unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return created.Select(i => i.ToDto(byId[i.MateriaId])).ToList();
    }

    /// <summary>Request-ordered materia ids, from ids directly or from codes (unknown codes -> 404).</summary>
    private async Task<IReadOnlyList<int>> ResolveMateriaIdsAsync(EnrollCommand request, CancellationToken ct)
    {
        if (request.CodigosMaterias is not { Count: > 0 } codigos)
            return request.MateriaIds ?? [];

        var map = await materias.GetIdsByCodigosAsync(codigos, ct);
        var unknown = codigos.Where(c => !map.ContainsKey(c)).ToList();
        if (unknown.Count > 0)
            throw new NotFoundException(
                AppErrorCodes.MateriaNotFound,
                $"No existe(n) la(s) materia(s) con código: {string.Join(", ", unknown)}.");

        return codigos.Select(c => map[c]).ToList();
    }
}
