using MediatR;
using Microsoft.AspNetCore.Mvc;
using MsInscripcion.Application.Features.Inscripciones.Dtos;
using MsInscripcion.Application.Features.Inscripciones.Queries;
using MsInscripcion.Application.Features.Materias.Dtos;
using MsInscripcion.Application.Features.Materias.Queries;

namespace MsInscripcion.Api.Controllers;

[ApiController]
[Route("api/estudiantes/{id:int}")]
[Produces("application/json")]
public class EstudiantesController(ISender mediator) : ControllerBase
{
    /// <summary>Inscripciones activas del estudiante en un período (por defecto el período actual).</summary>
    [HttpGet("inscripciones")]
    [ProducesResponseType(typeof(IReadOnlyList<InscripcionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInscripciones(int id, [FromQuery] string? periodo, CancellationToken ct) =>
        Ok(await mediator.Send(new GetInscripcionesQuery(id, periodo), ct));

    /// <summary>Materias que el estudiante puede inscribir hoy (informativo, sin bloqueo).</summary>
    [HttpGet("materias-disponibles")]
    [ProducesResponseType(typeof(IReadOnlyList<MateriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMateriasDisponibles(int id, [FromQuery] string? periodo, CancellationToken ct) =>
        Ok(await mediator.Send(new MateriasDisponiblesQuery(id, periodo), ct));
}
