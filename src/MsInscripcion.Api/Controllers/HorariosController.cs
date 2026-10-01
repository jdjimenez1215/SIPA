using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Horarios.Commands;
using MsInscripcion.Application.Features.Horarios.Dtos;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/materias/{materiaId:int}/horarios")]
[Produces("application/json")]
public class HorariosController(ISender mediator) : ControllerBase
{
    /// <summary>HoraInicio / HoraFin en formato HH:mm.</summary>
    public sealed record HorarioRequest(DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int materiaId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetHorariosByMateriaQuery(materiaId), ct));

    [HttpPost]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(int materiaId, [FromBody] HorarioRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(
            new AddHorarioCommand(materiaId, request.DiaSemana, request.HoraInicio, request.HoraFin), ct);
        return Created($"/api/materias/{materiaId}/horarios", created);
    }

    [HttpPut("{horarioId:int}")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        int materiaId, int horarioId, [FromBody] HorarioRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(
            new UpdateHorarioCommand(materiaId, horarioId, request.DiaSemana, request.HoraInicio, request.HoraFin), ct));

    [HttpDelete("{horarioId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int materiaId, int horarioId, CancellationToken ct)
    {
        await mediator.Send(new DeleteHorarioCommand(materiaId, horarioId), ct);
        return NoContent();
    }
}
