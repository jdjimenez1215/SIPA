using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Domain.Services;

/// <param name="Motivo">Violation code that blocks the row (null when <see cref="EstadoSugerencia.Sugerida"/>).</param>
public sealed record SuggestionRow(
    Materia Materia,
    EstadoSugerencia Estado,
    bool PrerrequisitoCumplido,
    string? Motivo);
