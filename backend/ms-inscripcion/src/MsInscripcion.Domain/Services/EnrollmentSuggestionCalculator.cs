using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.Domain.Services;

/// <summary>
/// Pure domain service. Walks the student's candidate window in priority order and asks the rules engine
/// about one materia at a time, so the suggestion and the POST path share exactly the same rules.
/// </summary>
public sealed class EnrollmentSuggestionCalculator(EnrollmentRulesEngine engine)
{
    private static readonly string[] MotivoPriority =
    [
        ErrorCodes.PrerequisiteNotMet,
        ErrorCodes.SeatsExhausted,
        ErrorCodes.ScheduleOverlap,
        ErrorCodes.ExtraSubjectsCapExceeded
    ];

    /// <param name="baseCtx">Context with empty <c>Candidates</c>.</param>
    /// <param name="careerMaterias">Materias of the student's career (other careers are ignored).</param>
    public IReadOnlyList<SuggestionRow> Suggest(EnrollmentContext baseCtx, IEnumerable<Materia> careerMaterias)
    {
        var n = baseCtx.Student.SemestreActual;
        var activeIds = baseCtx.ActiveEnrolledMaterias.Select(m => m.Id).ToHashSet();

        var evalOrder = careerMaterias
            .Where(m => m.CarreraId == baseCtx.Student.CarreraId
                        && m.Semestre <= n + 1
                        && !baseCtx.ApprovedMateriaIds.Contains(m.Id)
                        && !activeIds.Contains(m.Id))
            .OrderBy(m => m.Semestre == n ? 0 : m.Semestre < n ? 1 : 2)
            .ThenBy(m => m.Codigo, StringComparer.Ordinal)
            .ToList();

        var accepted = new List<Materia>();
        var rows = new List<SuggestionRow>();

        foreach (var materia in evalOrder)
        {
            var ctx = baseCtx with { Candidates = [.. accepted, materia] };
            var violations = engine.EvaluateCandidate(ctx, materia);

            if (violations.Count == 0)
            {
                accepted.Add(materia);
                rows.Add(new SuggestionRow(materia, EstadoSugerencia.Sugerida, true, null));
                continue;
            }

            rows.Add(new SuggestionRow(
                materia,
                EstadoSugerencia.Prerrequisito,
                violations.All(v => v.Code != ErrorCodes.PrerequisiteNotMet),
                PickMotivo(violations)));
        }

        return rows
            .OrderBy(r => r.Materia.Semestre)
            .ThenBy(r => r.Materia.Codigo, StringComparer.Ordinal)
            .ToList();
    }

    private static string PickMotivo(IReadOnlyList<RuleViolation> violations)
    {
        foreach (var code in MotivoPriority)
            if (violations.Any(v => v.Code == code))
                return code;

        return violations[0].Code;
    }
}
