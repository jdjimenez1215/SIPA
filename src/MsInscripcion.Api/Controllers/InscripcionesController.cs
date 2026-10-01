using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Inscripciones.Commands;
using MsInscripcion.Application.Features.Inscripciones.Dtos;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/inscripciones")]
[Produces("application/json")]
public class InscripcionesController(ISender mediator) : ControllerBase
{
    public sealed record EnrollRequest(int EstudianteId, string Periodo, IReadOnlyList<int> MateriaIds);

    /// <summary>Inscribe al estudiante en una o más materias (todo o nada).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<InscripcionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new EnrollCommand(request.EstudianteId, request.Periodo, request.MateriaIds ?? []), ct);
        return Created($"/api/estudiantes/{request.EstudianteId}/inscripciones?periodo={request.Periodo}", result);
    }

    /// <summary>Cancela una inscripción activa (cancelación lógica).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await mediator.Send(new CancelCommand(id), ct);
        return NoContent();
    }
}
