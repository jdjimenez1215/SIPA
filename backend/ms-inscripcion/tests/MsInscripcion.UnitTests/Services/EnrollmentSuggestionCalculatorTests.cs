using FluentAssertions;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Services;

public class EnrollmentSuggestionCalculatorTests
{
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

    // Student in semester 3: N = 3, owed = 1..2, N+1 = 4.
    private static IReadOnlyList<SuggestionRow> Suggest(
        IEnumerable<Materia> materias,
        int semestreActual = 3,
        int maxExtraSubjects = 3,
        IEnumerable<int>? approved = null,
        IEnumerable<Materia>? active = null,
        IDictionary<int, int>? seats = null)
    {
        var ctx = TestData.Context(
            [],
            TestData.Student(semestreActual: semestreActual),
            maxExtraSubjects,
            approved,
            active,
            seats);

        return Calculator.Suggest(ctx, materias);
    }

    private static Materia M(
        int id,
        string codigo,
        int semestre,
        int cupos = 30,
        IEnumerable<HorarioMateria>? horarios = null,
        IEnumerable<int>? prerequisiteIds = null,
        int carreraId = 1) =>
        TestData.Materia(id, carreraId, semestre, cupos, codigo, horarios, prerequisiteIds);

    private static HorarioMateria Slot(DiaSemana day, int start, int end) => TestData.Block(day, start, end);

    private static string[] Codes(IEnumerable<SuggestionRow> rows, EstadoSugerencia estado) =>
        rows.Where(r => r.Estado == estado).Select(r => r.Materia.Codigo).ToArray();

    private static SuggestionRow Row(IEnumerable<SuggestionRow> rows, string codigo) =>
        rows.Single(r => r.Materia.Codigo == codigo);

    [Fact]
    public void Suggest_CurrentSemesterSubjects_AreAllSuggestedWithoutMotivo()
    {
        var rows = Suggest([M(1, "C1", 3), M(2, "C2", 3), M(3, "C3", 3), M(4, "C4", 3), M(5, "C5", 3)]);

        rows.Should().HaveCount(5);
        rows.Should().OnlyContain(r => r.Estado == EstadoSugerencia.Sugerida
                                       && r.PrerrequisitoCumplido
                                       && r.Motivo == null);
    }

    [Fact]
    public void Suggest_NextSemesterBeyondCap_ReturnsCapRowsAsPrerrequisitoWithPrerequisiteMet()
    {
        var rows = Suggest([M(1, "N1", 4), M(2, "N2", 4), M(3, "N3", 4), M(4, "N4", 4), M(5, "N5", 4)]);

        Codes(rows, EstadoSugerencia.Sugerida).Should().Equal("N1", "N2", "N3");
        foreach (var codigo in new[] { "N4", "N5" })
        {
            var row = Row(rows, codigo);
            row.Estado.Should().Be(EstadoSugerencia.Prerrequisito);
            row.PrerrequisitoCumplido.Should().BeTrue();
            row.Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        }
    }

    [Fact]
    public void Suggest_OwedSubjectsHavePriorityAndShareTheCapWithNextSemester()
    {
        // Owed codes sort AFTER the N+1 codes, but owed ones are evaluated first.
        var rows = Suggest(
        [
            M(1, "Z-OWED1", 1),
            M(2, "Z-OWED2", 2),
            M(3, "A-NEXT1", 4),
            M(4, "B-NEXT2", 4)
        ]);

        Codes(rows, EstadoSugerencia.Sugerida).Should().BeEquivalentTo("Z-OWED1", "Z-OWED2", "A-NEXT1");
        Row(rows, "B-NEXT2").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }

    [Fact]
    public void Suggest_OwedSubjectsAreNotLimitedBelowTheCapButStillCountTowardsIt()
    {
        var rows = Suggest(
        [
            M(1, "O1", 1), M(2, "O2", 1), M(3, "O3", 2), M(4, "O4", 2),
            M(5, "N1", 4)
        ]);

        // Four owed subjects: the 4th owed one already exceeds the shared cap of 3.
        Codes(rows, EstadoSugerencia.Sugerida).Should().Equal("O1", "O2", "O3");
        Row(rows, "O4").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        Row(rows, "N1").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }

    [Fact]
    public void Suggest_NextSemesterRowWithoutSeats_DoesNotConsumeASlot()
    {
        var rows = Suggest(
            [M(1, "N1", 4, cupos: 1), M(2, "N2", 4), M(3, "N3", 4), M(4, "N4", 4), M(5, "N5", 4)],
            seats: new Dictionary<int, int> { [1] = 1 });

        Row(rows, "N1").Motivo.Should().Be(ErrorCodes.SeatsExhausted);
        Row(rows, "N1").PrerrequisitoCumplido.Should().BeTrue();
        Codes(rows, EstadoSugerencia.Sugerida).Should().Equal("N2", "N3", "N4");
        Row(rows, "N5").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }

    [Fact]
    public void Suggest_RowFailingPrerequisite_DoesNotConsumeASlot()
    {
        var rows = Suggest(
            [M(1, "N1", 4, prerequisiteIds: [99]), M(2, "N2", 4), M(3, "N3", 4), M(4, "N4", 4)]);

        Row(rows, "N1").Estado.Should().Be(EstadoSugerencia.Prerrequisito);
        Codes(rows, EstadoSugerencia.Sugerida).Should().Equal("N2", "N3", "N4");
    }

    [Fact]
    public void Suggest_ClashWithAcceptedRow_ReturnsCruceHorario()
    {
        var rows = Suggest(
        [
            M(1, "C1", 3, horarios: [Slot(DiaSemana.Lunes, 8, 10)]),
            M(2, "N1", 4, horarios: [Slot(DiaSemana.Lunes, 9, 11)]),
            M(3, "N2", 4, horarios: [Slot(DiaSemana.Lunes, 10, 12)])
        ]);

        Row(rows, "N1").Motivo.Should().Be(ErrorCodes.ScheduleOverlap);
        Row(rows, "N1").PrerrequisitoCumplido.Should().BeTrue();
        Row(rows, "N2").Estado.Should().Be(EstadoSugerencia.Sugerida); // back-to-back is not a clash
    }

    [Fact]
    public void Suggest_ClashWithRejectedRow_IsNotAClash()
    {
        // N1 is rejected (no seats), so N2 sharing its slot must still be suggested.
        var rows = Suggest(
            [
                M(1, "N1", 4, cupos: 1, horarios: [Slot(DiaSemana.Martes, 8, 10)]),
                M(2, "N2", 4, horarios: [Slot(DiaSemana.Martes, 8, 10)])
            ],
            seats: new Dictionary<int, int> { [1] = 1 });

        Row(rows, "N1").Motivo.Should().Be(ErrorCodes.SeatsExhausted);
        Row(rows, "N2").Estado.Should().Be(EstadoSugerencia.Sugerida);
    }

    [Fact]
    public void Suggest_ActiveEnrollmentsAreHiddenButCountedTowardsCapAndClashes()
    {
        var active = new[]
        {
            M(10, "ACT1", 4, horarios: [Slot(DiaSemana.Lunes, 8, 10)]),
            M(11, "ACT2", 4),
            M(12, "ACT3", 3, horarios: [Slot(DiaSemana.Viernes, 8, 10)])   // current semester: not counted
        };

        var rows = Suggest(
            [
                .. active,
                M(1, "N1", 4),
                M(2, "N2", 4),
                M(3, "C1", 3, horarios: [Slot(DiaSemana.Lunes, 9, 10)]),
                M(4, "C2", 3, horarios: [Slot(DiaSemana.Viernes, 9, 11)])
            ],
            active: active);

        rows.Select(r => r.Materia.Codigo).Should().NotContain(new[] { "ACT1", "ACT2", "ACT3" });
        Row(rows, "N1").Estado.Should().Be(EstadoSugerencia.Sugerida);     // 2 active extras + N1 = 3
        Row(rows, "N2").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        Row(rows, "C1").Motivo.Should().Be(ErrorCodes.ScheduleOverlap);    // clashes with an active enrollment
        Row(rows, "C2").Motivo.Should().Be(ErrorCodes.ScheduleOverlap);
    }

    [Fact]
    public void Suggest_LastSemester_HasNoNextSemesterRows()
    {
        var rows = Suggest(
            [M(1, "S9", 9), M(2, "S10", 10)],
            semestreActual: 10);

        rows.Select(r => r.Materia.Semestre).Should().OnlyContain(s => s <= 10);
        Codes(rows, EstadoSugerencia.Sugerida).Should().BeEquivalentTo("S9", "S10");
    }

    [Fact]
    public void Suggest_PrerrequisitoCumplido_IsFalseOnlyWhenThePrerequisiteFails()
    {
        // N1..N3 fill the cap; X fails the prerequisite AND the cap; Y fails only the cap; Z has no seats.
        var rows = Suggest(
            [
                M(1, "N1", 4), M(2, "N2", 4), M(3, "N3", 4),
                M(4, "X", 4, prerequisiteIds: [99]),
                M(5, "Y", 4),
                M(6, "Z", 4, cupos: 1)
            ],
            seats: new Dictionary<int, int> { [6] = 1 });

        Row(rows, "X").PrerrequisitoCumplido.Should().BeFalse();
        Row(rows, "X").Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);
        Row(rows, "Y").PrerrequisitoCumplido.Should().BeTrue();
        Row(rows, "Z").PrerrequisitoCumplido.Should().BeTrue();
    }

    [Fact]
    public void Suggest_SeveralViolations_PicksMotivoByPriority()
    {
        // Y: prerequisite + seats + cap. W: seats + cap. V: clash + cap.
        var rows = Suggest(
            [
                M(1, "N1", 4, horarios: [Slot(DiaSemana.Lunes, 8, 10)]),
                M(2, "N2", 4), M(3, "N3", 4),
                M(4, "Y", 4, cupos: 1, prerequisiteIds: [99]),
                M(5, "W", 4, cupos: 1),
                M(6, "V", 4, horarios: [Slot(DiaSemana.Lunes, 9, 11)])
            ],
            seats: new Dictionary<int, int> { [4] = 1, [5] = 1 });

        Row(rows, "Y").Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);
        Row(rows, "W").Motivo.Should().Be(ErrorCodes.SeatsExhausted);
        Row(rows, "V").Motivo.Should().Be(ErrorCodes.ScheduleOverlap);
    }

    [Fact]
    public void Suggest_Window_ExcludesApprovedBeyondNextSemesterAndOtherCareers()
    {
        var rows = Suggest(
            [
                M(1, "APPROVED", 2),
                M(2, "TOO-FAR", 5),
                M(3, "OTHER", 3, carreraId: 2),
                M(4, "OK", 3)
            ],
            approved: [1]);

        rows.Select(r => r.Materia.Codigo).Should().Equal("OK");
    }

    [Fact]
    public void Suggest_Result_IsOrderedBySemesterThenCodigo()
    {
        var rows = Suggest(
            [M(1, "B-N1", 4), M(2, "A-C2", 3), M(3, "A-C1", 3), M(4, "Z-OWED", 1), M(5, "A-N1", 4)]);

        rows.Select(r => r.Materia.Codigo).Should().Equal("Z-OWED", "A-C1", "A-C2", "A-N1", "B-N1");
    }
}
