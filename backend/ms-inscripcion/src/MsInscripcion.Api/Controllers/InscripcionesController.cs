using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Api.Security;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Features.Inscripciones.Commands;
using MsInscripcion.Application.Features.Inscripciones.Dtos;

namespace MsInscripcion.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/inscripciones")]
public class InscripcionesController(ISender mediator, IInscripcionRepository inscripciones) : ControllerBase
{
    public sealed record EnrollRequest(int EstudianteId, string Periodo,
        IReadOnlyList<int>? MateriaIds, IReadOnlyList<string>? CodigosMaterias);

    /// <summary>Inscribe al estudiante en una o más materias (todo o nada).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<InscripcionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken ct)
    {
        if (!StudentAccess.CanAccess(User, request.EstudianteId))
            return Forbid();

        var result = await mediator.Send(
            new EnrollCommand(request.EstudianteId, request.Periodo, request.MateriaIds, request.CodigosMaterias), ct);
        return Created($"/api/estudiantes/{request.EstudianteId}/inscripciones?periodo={request.Periodo}", result);
    }

    /// <summary>Cancela una inscripción activa (cancelación lógica).</summary>
    /// <summary>Cancela una inscripción activa. Un estudiante solo puede cancelar las suyas; Admin, cualquiera.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var inscripcion = await inscripciones.GetByIdAsync(id, ct);
        if (inscripcion is not null && !StudentAccess.CanAccess(User, inscripcion.EstudianteId))
            return Forbid();

        await mediator.Send(new CancelCommand(id), ct);
        return NoContent();
    }
}
