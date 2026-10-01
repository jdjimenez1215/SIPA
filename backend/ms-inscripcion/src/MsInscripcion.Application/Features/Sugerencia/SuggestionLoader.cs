using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.Application.Features.Sugerencia;

/// <summary>
/// Loads the read-only inputs (no locks, advisory) and runs the domain calculator.
/// Shared by the suggestion query and the legacy available-subjects query.
/// </summary>
internal static class SuggestionLoader
{
    public static async Task<IReadOnlyList<SuggestionRow>> LoadAsync(
        Estudiante student,
        string period,
        int maxExtraSubjects,
        IStudentRepository students,
        IMateriaRepository materias,
        IInscripcionRepository inscripciones,
        EnrollmentSuggestionCalculator calculator,
        CancellationToken ct)
    {
        var approved = await students.GetApprovedMateriaIdsAsync(student.Id, ct);
        var careerMaterias = await materias.GetByCarreraAsync(student.CarreraId, ct);
        if (careerMaterias.Count == 0)
            return [];

        var activeEnrollments = await inscripciones.GetActiveWithScheduleAsync(student.Id, period, ct);
        var seatCounts = await inscripciones.CountActiveAsync(careerMaterias.Select(m => m.Id).ToList(), period, ct);

        var baseContext = new EnrollmentContext(
            student,
            period,
            maxExtraSubjects,
            approved,
            activeEnrollments.Where(i => i.Materia is not null).Select(i => i.Materia!).ToList(),
            seatCounts,
            []);

        return calculator.Suggest(baseContext, careerMaterias);
    }
}
