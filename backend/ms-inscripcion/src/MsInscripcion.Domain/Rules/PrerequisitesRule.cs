using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Domain.Rules;

public sealed class PrerequisitesRule : IEnrollmentRule
{
    public IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate)
    {
        var missing = candidate.Prerrequisitos
            .Where(p => !ctx.ApprovedMateriaIds.Contains(p.MateriaRequisitoId))
            .ToList();

        if (missing.Count == 0)
            yield break;

        // Codigo is available when the navigation is loaded; fall back to the id otherwise.
        var missingCodigos = missing
            .Select(p => p.MateriaRequisito?.Codigo ?? p.MateriaRequisitoId.ToString())
            .ToList();

        yield return new RuleViolation(
            ErrorCodes.PrerequisiteNotMet,
            candidate.Id,
            candidate.Codigo,
            $"La materia {candidate.Codigo} requiere aprobar: {string.Join(", ", missingCodigos)}.",
            new Dictionary<string, object>
            {
                ["missing"] = missingCodigos,
                ["missingIds"] = missing.Select(p => p.MateriaRequisitoId).ToList()
            });
    }
}
