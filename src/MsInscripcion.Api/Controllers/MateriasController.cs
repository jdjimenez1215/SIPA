using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Materias.Commands;
using MsInscripcion.Application.Features.Materias.Dtos;
using MsInscripcion.Application.Features.Materias.Queries;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/materias")]
public class MateriasController(ISender mediator) : ControllerBase
{
    public sealed record MateriaRequest(
        string Codigo, string Nombre, int Creditos, int CarreraId, int Semestre, int CuposMaximos);

    /// <summary>Catálogo de materias con horarios y prerrequisitos, filtrable por carrera y semestre.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MateriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int? carreraId, [FromQuery] int? semestre, CancellationToken ct) =>
        Ok(await mediator.Send(new GetMateriasQuery(carreraId, semestre), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetMateriaByIdQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] MateriaRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(
            new CreateMateriaCommand(
                request.Codigo, request.Nombre, request.Creditos, request.CarreraId, request.Semestre, request.CuposMaximos),
            ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] MateriaRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(
            new UpdateMateriaCommand(
                id, request.Codigo, request.Nombre, request.Creditos, request.CarreraId, request.Semestre, request.CuposMaximos),
            ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteMateriaCommand(id), ct);
        return NoContent();
    }
}
