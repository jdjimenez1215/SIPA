using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Prerrequisitos.Commands;
using MsInscripcion.Application.Features.Prerrequisitos.Dtos;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/materias/{materiaId:int}/prerrequisitos")]
public class PrerrequisitosController(ISender mediator) : ControllerBase
{
    public sealed record PrerrequisitoRequest(int MateriaRequisitoId);

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PrerrequisitoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int materiaId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPrerrequisitosByMateriaQuery(materiaId), ct));

    [HttpPost]
    [ProducesResponseType(typeof(PrerrequisitoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(int materiaId, [FromBody] PrerrequisitoRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new AddPrerrequisitoCommand(materiaId, request.MateriaRequisitoId), ct);
        return Created($"/api/materias/{materiaId}/prerrequisitos", created);
    }

    [HttpDelete("{materiaRequisitoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int materiaId, int materiaRequisitoId, CancellationToken ct)
    {
        await mediator.Send(new DeletePrerrequisitoCommand(materiaId, materiaRequisitoId), ct);
        return NoContent();
    }
}
