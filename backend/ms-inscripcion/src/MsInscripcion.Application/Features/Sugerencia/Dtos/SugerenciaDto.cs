using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Application.Features.Sugerencia.Dtos;

public sealed record SugerenciaDto(
    string NombreEstudiante,
    string Programa,
    int SemestreActual,
    string Periodo,
    int TotalCreditos,
    IReadOnlyList<MateriaSugeridaDto> MateriasSugeridas,
    IReadOnlyList<NotificacionDto> Notificaciones);

/// <param name="Estado">Serialized as string (JsonStringEnumConverter).</param>
/// <param name="Motivo">Rule code that blocks the row; null when <c>Sugerida</c>.</param>
public sealed record MateriaSugeridaDto(
    int Id,
    string Codigo,
    string Nombre,
    int Creditos,
    int Semestre,
    EstadoSugerencia Estado,
    bool PrerrequisitoCumplido,
    string? Motivo);

public sealed record NotificacionDto(
    int Id,
    string Titulo,
    string Mensaje,
    DateTimeOffset Fecha,
    bool Leida);
