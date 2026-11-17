using System.Globalization;
using System.Text;

namespace MsAsistente.Api;

public sealed class AsistenteService(IAsistenteLlm llm, ILogger<AsistenteService> logger)
{
    public async Task<AsistenteRespuesta> ResponderAsync(SugerenciaDto sugerencia, string mensaje, CancellationToken ct)
    {
        var sugeridas = sugerencia.MateriasSugeridas
            .Where(m => string.Equals(m.Estado, "Sugerida", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var bloqueadas = sugerencia.MateriasSugeridas
            .Where(m => !string.Equals(m.Estado, "Sugerida", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var (sugeridasFiltradas, diasExcluidos) = FiltrarPorDias(sugeridas, mensaje);
        var notaDias = diasExcluidos.Count > 0
            ? $" Excluí {string.Join(", ", diasExcluidos)} según tu preferencia."
            : string.Empty;

        var explicaciones = bloqueadas
            .Select(m => new ExplicacionDto(m.Codigo, m.Motivo ?? "BLOQUEADA", Explicar(m)))
            .ToList();

        if (sugeridasFiltradas.Count == 0 && diasExcluidos.Count > 0)
        {
            return new AsistenteRespuesta(
                "Con los días que pediste no quedan materias sugeridas que puedas inscribir." + notaDias,
                [], explicaciones, false, sugerencia.LimiteCreditos);
        }

        var contexto = new AsistenteContexto(
            sugerencia.NombreEstudiante,
            sugerencia.SemestreActual,
            sugerencia.Promedio,
            sugerencia.LimiteCreditos,
            sugeridasFiltradas,
            bloqueadas,
            mensaje ?? string.Empty);

        var fallback = false;
        LlmPropuesta propuesta;
        try
        {
            propuesta = await llm.ProponerAsync(contexto, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "La IA no respondió; se usa el modo de reglas.");
            propuesta = await new ReglasAsistenteLlm().ProponerAsync(contexto, ct);
            fallback = true;
        }

        var ids = Sanear(propuesta.MateriaIds, sugeridasFiltradas, sugerencia.LimiteCreditos);
        if (ids.Count == 0 && sugeridasFiltradas.Count > 0)
        {
            var reglas = await new ReglasAsistenteLlm().ProponerAsync(contexto, ct);
            ids = Sanear(reglas.MateriaIds, sugeridasFiltradas, sugerencia.LimiteCreditos);
            propuesta = reglas;
            fallback = true;
        }

        var texto = string.IsNullOrWhiteSpace(propuesta.Mensaje)
            ? "Revisá las materias propuestas y confirmá la matrícula."
            : propuesta.Mensaje;
        texto += notaDias;

        return new AsistenteRespuesta(texto, ids, explicaciones, fallback, sugerencia.LimiteCreditos);
    }

    /// <summary>Only suggested IDs, without duplicates, respecting the credit limit.</summary>
    private static List<int> Sanear(
        IReadOnlyList<int> propuestos, IReadOnlyList<MateriaSugerida> sugeridas, int limiteCreditos)
    {
        var byId = sugeridas.ToDictionary(m => m.Id);
        var ids = new List<int>();
        var creditos = 0;
        foreach (var id in (propuestos ?? []).Distinct())
        {
            if (!byId.TryGetValue(id, out var materia))
                continue;
            if (creditos + materia.Creditos > limiteCreditos)
                continue;

            ids.Add(id);
            creditos += materia.Creditos;
        }
        return ids;
    }

    private static readonly (string Key, string Label)[] DayNames =
    [
        ("lunes", "Lunes"), ("martes", "Martes"), ("miercoles", "Miércoles"),
        ("jueves", "Jueves"), ("viernes", "Viernes"), ("sabado", "Sábado"), ("domingo", "Domingo")
    ];

    /// <summary>If the student says "no quiero/no puedo ... lunes", drop subjects with a block that day.</summary>
    private static (List<MateriaSugerida> Materias, List<string> Dias) FiltrarPorDias(
        IReadOnlyList<MateriaSugerida> sugeridas, string mensaje)
    {
        var normalized = Normalize(mensaje ?? string.Empty);
        var negative = normalized.Contains("no quiero") || normalized.Contains("no puedo")
                       || normalized.Contains("no deseo") || normalized.Contains("evitar")
                       || normalized.Contains("sin clases") || normalized.Contains("no estudiar");
        if (!negative)
            return (sugeridas.ToList(), []);

        var excluded = DayNames
            .Where(day => normalized.Contains(day.Key))
            .Select(day => day.Key)
            .ToHashSet();
        if (excluded.Count == 0)
            return (sugeridas.ToList(), []);

        var filtered = sugeridas
            .Where(m => !(m.Horarios ?? []).Any(h => excluded.Contains(Normalize(h.DiaSemana))))
            .ToList();

        var labels = DayNames.Where(d => excluded.Contains(d.Key)).Select(d => d.Label).ToList();
        return (filtered, labels);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    private static string Explicar(MateriaSugerida materia) => materia.Motivo switch
    {
        "PREREQUISITO_NO_CUMPLIDO" => "Falta aprobar una materia prerrequisito.",
        "LIMITE_CREDITOS_EXCEDIDO" => "Ya se alcanzó el límite de créditos del período.",
        "LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO" => "Se alcanzó el cupo de materias extra.",
        "CRUCE_HORARIO" => "Se cruza con otra materia de tu horario.",
        "CUPO_AGOTADO" => "No quedan cupos disponibles.",
        "MATERIA_YA_INSCRITA" => "Ya está inscrita en este período.",
        "MATERIA_YA_APROBADA" => "Ya está aprobada.",
        "SEMESTRE_EXCEDIDO" => "Está fuera de la ventana de semestres permitida.",
        _ => "La materia está bloqueada por una regla de inscripción."
    };
}
