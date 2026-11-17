using System.Text.Json;
using System.Text.Json.Serialization;

namespace MsAsistente.Api;

public sealed record ChatRequest(string Mensaje, string? Periodo = null);

public sealed record ExplicacionDto(string Codigo, string Motivo, string Mensaje);

public sealed record AsistenteRespuesta(
    string Mensaje,
    IReadOnlyList<int> MateriaIds,
    IReadOnlyList<ExplicacionDto> Explicaciones,
    bool Fallback,
    int LimiteCreditos);

public sealed record HorarioDto(string DiaSemana, string HoraInicio, string HoraFin);

public sealed record MateriaSugerida(
    int Id,
    string Codigo,
    string Nombre,
    int Creditos,
    int Semestre,
    string Estado,
    bool PrerrequisitoCumplido,
    string? Motivo,
    int CuposDisponibles,
    int CuposMaximos,
    IReadOnlyList<HorarioDto>? Horarios = null);

public sealed record SugerenciaDto(
    string NombreEstudiante,
    string Programa,
    int SemestreActual,
    string Periodo,
    int TotalCreditos,
    decimal Promedio,
    int LimiteCreditos,
    IReadOnlyList<MateriaSugerida> MateriasSugeridas);

public sealed record LlmPropuesta(string Mensaje, IReadOnlyList<int> MateriaIds);

public sealed record AsistenteContexto(
    string NombreEstudiante,
    int SemestreActual,
    decimal Promedio,
    int LimiteCreditos,
    IReadOnlyList<MateriaSugerida> Sugeridas,
    IReadOnlyList<MateriaSugerida> Bloqueadas,
    string MensajeEstudiante);

public static class JsonDefaults
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
