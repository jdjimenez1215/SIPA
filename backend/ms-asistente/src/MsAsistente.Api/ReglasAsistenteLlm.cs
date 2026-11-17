namespace MsAsistente.Api;

public interface IAsistenteLlm
{
    /// <summary>Returns a proposal for the student's current suggestion. Never enrolls.</summary>
    Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default);
}

/// <summary>
/// Deterministic fallback: recommends the suggested rows (already ordered oldest-first)
/// until the credit limit, and explains blocked rows when asked.
/// </summary>
public sealed class ReglasAsistenteLlm : IAsistenteLlm
{
    public Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default)
    {
        var ids = new List<int>();
        var creditos = 0;
        foreach (var materia in contexto.Sugeridas)
        {
            if (creditos + materia.Creditos > contexto.LimiteCreditos)
                continue;

            ids.Add(materia.Id);
            creditos += materia.Creditos;
        }

        var preguntaBloqueos = contexto.MensajeEstudiante.Contains("bloque", StringComparison.OrdinalIgnoreCase)
            || contexto.MensajeEstudiante.Contains("por qué", StringComparison.OrdinalIgnoreCase)
            || contexto.MensajeEstudiante.Contains("porque", StringComparison.OrdinalIgnoreCase);

        var mensaje = ids.Count == 0
            ? "No encontré materias sugeridas que quepan en tu límite de créditos."
            : $"Te propongo {ids.Count} materia(s) para un total de {creditos} créditos " +
              $"(límite {contexto.LimiteCreditos}). Revisá las casillas y confirmá vos mismo la matrícula.";

        if (preguntaBloqueos && contexto.Bloqueadas.Count > 0)
            mensaje += $" Tenés {contexto.Bloqueadas.Count} materia(s) bloqueada(s); abajo está el motivo de cada una.";

        return Task.FromResult(new LlmPropuesta(mensaje, ids));
    }
}
