using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Carreras.Commands;
using MsInscripcion.Application.Features.Carreras.Dtos;
using MsInscripcion.Application.Features.Carreras.Queries;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/carreras")]
[Produces("application/json")]
public class CarrerasController(ISender mediator) : ControllerBase
{
    public sealed record CarreraRequest(string Codigo, string Nombre, int DuracionSemestres);

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CarreraDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new GetCarrerasQuery(), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CarreraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCarreraByIdQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(typeof(CarreraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CarreraRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(
            new CreateCarreraCommand(request.Codigo, request.Nombre, request.DuracionSemestres), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarreraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] CarreraRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(
            new UpdateCarreraCommand(id, request.Codigo, request.Nombre, request.DuracionSemestres), ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCarreraCommand(id), ct);
        return NoContent();
    }
}
