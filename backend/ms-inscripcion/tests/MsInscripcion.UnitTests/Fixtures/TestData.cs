using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.UnitTests.Fixtures;

/// <summary>Concise builders for entities and <see cref="EnrollmentContext"/> used across the unit tests.</summary>
public static class TestData
{
    public const string Period = "2026-2";

    public static TimeOnly T(int hour, int minute = 0) => new(hour, minute);

    public static HorarioMateria Block(DiaSemana day, int startHour, int endHour, int materiaId = 0) => new()
    {
        MateriaId = materiaId,
        DiaSemana = day,
        HoraInicio = T(startHour),
        HoraFin = T(endHour)
    };

    public static Materia Materia(
        int id,
        int carreraId = 1,
        int semestre = 1,
        int cupos = 30,
        string? codigo = null,
        IEnumerable<HorarioMateria>? horarios = null,
        IEnumerable<int>? prerequisiteIds = null)
    {
        var materia = new Materia
        {
            Id = id,
            Codigo = codigo ?? $"MAT{id}",
            Nombre = $"Materia {id}",
            Creditos = 3,
            CarreraId = carreraId,
            Semestre = semestre,
            CuposMaximos = cupos
        };

        foreach (var h in horarios ?? [])
        {
            h.MateriaId = id;
            materia.Horarios.Add(h);
        }

        foreach (var reqId in prerequisiteIds ?? [])
            materia.Prerrequisitos.Add(new Prerrequisito { MateriaId = id, MateriaRequisitoId = reqId });

        return materia;
    }

    public static Estudiante Student(int id = 1, int carreraId = 1, int semestreActual = 2) => new()
    {
        Id = id,
        Nombre = $"Estudiante {id}",
        CarreraId = carreraId,
        SemestreActual = semestreActual
    };

    public static EnrollmentContext Context(
        Materia[] candidates,
        Estudiante? student = null,
        int maxExtraSubjects = 3,
        IEnumerable<int>? approved = null,
        IEnumerable<Materia>? activeEnrolled = null,
        IDictionary<int, int>? seatCounts = null) =>
        new(
            student ?? Student(),
            Period,
            maxExtraSubjects,
            new HashSet<int>(approved ?? []),
            (activeEnrolled ?? []).ToList(),
            new Dictionary<int, int>(seatCounts ?? new Dictionary<int, int>()),
            candidates);

    public static List<RuleViolation> Run(IEnrollmentRule rule, EnrollmentContext ctx, Materia candidate) =>
        rule.Evaluate(ctx, candidate).ToList();
}
