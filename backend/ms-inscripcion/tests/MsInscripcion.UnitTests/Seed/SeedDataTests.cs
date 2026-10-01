using FluentAssertions;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.UnitTests.Seed;

/// <summary>Seed integrity plus the real calculator over the real seed (the demo scenarios).</summary>
public class SeedDataTests
{
    private const string Period = "2026-2";
    private const int Laura = 1, Mateo = 2, Camila = 3, Sofia = 4;

    private static readonly EnrollmentSuggestionCalculator Calculator = new(new EnrollmentRulesEngine(new IEnrollmentRule[]
    {
        new CareerMatchRule(),
        new PrerequisitesRule(),
        new ScheduleOverlapRule(),
        new SemesterWindowRule(),
        new ExtraSubjectsCapRule(),
        new NoDuplicateRule(),
        new SeatAvailabilityRule()
    }));

    [Fact]
    public void Materias_Has54RowsWith53CareerSubjectsAndOneOtherCareer()
    {
        SeedData.Materias.Should().HaveCount(54);
        SeedData.Materias.Count(m => m.CarreraId == 1).Should().Be(53);
        SeedData.Materias.Count(m => m.CarreraId == 2).Should().Be(1);
    }

    [Fact]
    public void Materias_HaveUniqueIdsAndCodigos()
    {
        SeedData.Materias.Select(m => m.Id).Should().OnlyHaveUniqueItems();
        SeedData.Materias.Select(m => m.Codigo).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Prerrequisitos_Has33RowsReferencingExistingMaterias()
    {
        var ids = SeedData.Materias.Select(m => m.Id).ToHashSet();

        SeedData.Prerrequisitos.Should().HaveCount(33);
        SeedData.Prerrequisitos.Should().OnlyContain(p =>
            ids.Contains(p.MateriaId) && ids.Contains(p.MateriaRequisitoId) && p.MateriaId != p.MateriaRequisitoId);
        SeedData.Prerrequisitos.Select(p => (p.MateriaId, p.MateriaRequisitoId)).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Prerrequisitos_RequiredSubjectBelongsToAnEarlierSemester()
    {
        var byId = SeedData.Materias.ToDictionary(m => m.Id);

        SeedData.Prerrequisitos.Should().OnlyContain(p =>
            byId[p.MateriaRequisitoId].Semestre < byId[p.MateriaId].Semestre);
    }

    [Fact]
    public void Horarios_ReferenceExistingMateriasAndHaveValidRanges()
    {
        var ids = SeedData.Materias.Select(m => m.Id).ToHashSet();

        SeedData.Horarios.Select(h => h.Id).Should().OnlyHaveUniqueItems();
        SeedData.Horarios.Should().OnlyContain(h => ids.Contains(h.MateriaId) && h.HoraInicio < h.HoraFin);
    }

    [Fact]
    public void Laura_Suggestion_HasTwelveRowsAndTwentyThreeCredits()
    {
        var rows = Suggest(Laura);

        rows.Should().HaveCount(12);
        Suggested(rows).Should().BeEquivalentTo(
            "603601", "603602", "603603", "603604", "603605", "603606", "603701", "603703");
        TotalCredits(rows).Should().Be(23);
        rows.Where(r => r.Estado == EstadoSugerencia.Prerrequisito)
            .Select(r => r.Materia.Codigo)
            .Should().BeEquivalentTo("603702", "603704", "603705", "603706");
    }

    [Fact]
    public void Laura_BlockedRows_HaveUnmetPrerequisite()
    {
        var rows = Suggest(Laura);

        rows.Where(r => r.Estado == EstadoSugerencia.Prerrequisito).Should().OnlyContain(r =>
            !r.PrerrequisitoCumplido && r.Motivo == ErrorCodes.PrerequisiteNotMet);
    }

    [Fact]
    public void Mateo_Suggestion_FillsTheCapAndFlagsUnmetPrerequisite()
    {
        var rows = Suggest(Mateo);

        Suggested(rows).Should().BeEquivalentTo(
            "603701", "603702", "603703", "603704", "603705", "603706",
            "603801", "603802", "603803");

        foreach (var codigo in new[] { "603804", "603806" })
        {
            var row = Row(rows, codigo);
            row.Estado.Should().Be(EstadoSugerencia.Prerrequisito);
            row.PrerrequisitoCumplido.Should().BeTrue();
            row.Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        }

        var blocked = Row(rows, "603805");
        blocked.Estado.Should().Be(EstadoSugerencia.Prerrequisito);
        blocked.PrerrequisitoCumplido.Should().BeFalse();
        blocked.Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);
    }

    [Fact]
    public void Camila_Suggestion_OwedSubjectSharesTheCap()
    {
        var rows = Suggest(Camila);

        // Semester 7 (N) is free of the cap; owed 603305 + 603801 + 603802 fill it.
        Suggested(rows).Should().BeEquivalentTo(
            "603305",
            "603701", "603702", "603703", "603704", "603705", "603706",
            "603801", "603802");

        foreach (var codigo in new[] { "603803", "603804", "603806" })
        {
            var row = Row(rows, codigo);
            row.Estado.Should().Be(EstadoSugerencia.Prerrequisito);
            row.PrerrequisitoCumplido.Should().BeTrue();
            row.Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        }

        Row(rows, "603305").Materia.Semestre.Should().Be(3);

        var blocked = Row(rows, "603805");
        blocked.PrerrequisitoCumplido.Should().BeFalse();
        blocked.Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);
    }

    [Fact]
    public void Sofia_Suggestion_ShowsPrerequisiteClashAndSeatReasons()
    {
        var rows = Suggest(Sofia);

        Suggested(rows).Should().BeEquivalentTo(
            "603801", "603802", "603803", "603804", "603805", "603806", "603904", "603905");

        var prerequisite = Row(rows, "603901");
        prerequisite.PrerrequisitoCumplido.Should().BeFalse();
        prerequisite.Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);

        var clash = Row(rows, "603902");
        clash.PrerrequisitoCumplido.Should().BeTrue();
        clash.Motivo.Should().Be(ErrorCodes.ScheduleOverlap);

        var seats = Row(rows, "603903");
        seats.PrerrequisitoCumplido.Should().BeTrue();
        seats.Motivo.Should().Be(ErrorCodes.SeatsExhausted);
    }

    [Fact]
    public void Suggestion_ForEveryStudent_NeverExceedsTheCapOfExtras()
    {
        foreach (var student in SeedData.Estudiantes)
        {
            var rows = Suggest(student.Id);
            var extras = rows.Count(r =>
                r.Estado == EstadoSugerencia.Sugerida && r.Materia.Semestre != student.SemestreActual);

            extras.Should().BeLessThanOrEqualTo(3, because: student.Nombre);
        }
    }

    // ---- helpers: build the same graph the repositories would load from the database ----

    private static IReadOnlyList<SuggestionRow> Suggest(int studentId)
    {
        var student = SeedData.Estudiantes.Single(e => e.Id == studentId);
        var materias = BuildMaterias();
        var byId = materias.ToDictionary(m => m.Id);

        var approved = SeedData.Historial
            .Where(h => h.EstudianteId == studentId && h.Estado == EstadoHistorial.Aprobada)
            .Select(h => h.MateriaId)
            .ToHashSet();

        var activeInscripciones = SeedData.Inscripciones
            .Where(i => i.PeriodoAcademico == Period && i.Estado == EstadoInscripcion.Activa)
            .ToList();

        var active = activeInscripciones
            .Where(i => i.EstudianteId == studentId)
            .Select(i => byId[i.MateriaId])
            .ToList();

        var seatCounts = activeInscripciones
            .GroupBy(i => i.MateriaId)
            .ToDictionary(g => g.Key, g => g.Count());

        var baseContext = new EnrollmentContext(student, Period, 3, approved, active, seatCounts, []);

        return Calculator.Suggest(baseContext, materias.Where(m => m.CarreraId == student.CarreraId));
    }

    /// <summary>Fresh copies (the seed arrays are shared static state) with Horarios and Prerrequisitos attached.</summary>
    private static List<Materia> BuildMaterias()
    {
        var materias = SeedData.Materias.Select(m => new Materia
        {
            Id = m.Id,
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Creditos = m.Creditos,
            CarreraId = m.CarreraId,
            Semestre = m.Semestre,
            CuposMaximos = m.CuposMaximos
        }).ToList();

        var byId = materias.ToDictionary(m => m.Id);

        foreach (var h in SeedData.Horarios)
        {
            byId[h.MateriaId].Horarios.Add(new HorarioMateria
            {
                Id = h.Id,
                MateriaId = h.MateriaId,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin
            });
        }

        foreach (var p in SeedData.Prerrequisitos)
        {
            byId[p.MateriaId].Prerrequisitos.Add(new Prerrequisito
            {
                MateriaId = p.MateriaId,
                MateriaRequisitoId = p.MateriaRequisitoId,
                MateriaRequisito = byId[p.MateriaRequisitoId]
            });
        }

        return materias;
    }

    private static string[] Suggested(IEnumerable<SuggestionRow> rows) =>
        rows.Where(r => r.Estado == EstadoSugerencia.Sugerida).Select(r => r.Materia.Codigo).ToArray();

    private static int TotalCredits(IEnumerable<SuggestionRow> rows) =>
        rows.Where(r => r.Estado == EstadoSugerencia.Sugerida).Sum(r => r.Materia.Creditos);

    private static SuggestionRow Row(IEnumerable<SuggestionRow> rows, string codigo) =>
        rows.Single(r => r.Materia.Codigo == codigo);
}
