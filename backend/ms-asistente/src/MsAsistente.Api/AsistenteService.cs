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

        var contexto = new AsistenteContexto(
            sugerencia.NombreEstudiante,
            sugerencia.SemestreActual,
            sugerencia.Promedio,
            sugerencia.LimiteCreditos,
            sugeridas,
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

        var ids = Sanear(propuesta.MateriaIds, sugeridas, sugerencia.LimiteCreditos);
        if (ids.Count == 0 && sugeridas.Count > 0)
        {
            var reglas = await new ReglasAsistenteLlm().ProponerAsync(contexto, ct);
            ids = Sanear(reglas.MateriaIds, sugeridas, sugerencia.LimiteCreditos);
            propuesta = reglas;
            fallback = true;
        }

        var explicaciones = bloqueadas
            .Select(m => new ExplicacionDto(m.Codigo, m.Motivo ?? "BLOQUEADA", Explicar(m)))
            .ToList();

        var texto = string.IsNullOrWhiteSpace(propuesta.Mensaje)
            ? "Revisá las materias propuestas y confirmá la matrícula."
            : propuesta.Mensaje;

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
