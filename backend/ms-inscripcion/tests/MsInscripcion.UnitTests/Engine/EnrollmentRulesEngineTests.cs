using FluentAssertions;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Engine;

public class EnrollmentRulesEngineTests
{
    private static EnrollmentRulesEngine BuildEngine() => new(new IEnrollmentRule[]
    {
        new CareerMatchRule(),
        new PrerequisitesRule(),
        new ScheduleOverlapRule(),
        new SemesterWindowRule(),
        new ExtraSubjectsCapRule(),
        new NoDuplicateRule(),
        new SeatAvailabilityRule()
    });

    [Fact]
    public void Evaluate_ValidRequest_ReturnsNoViolations()
    {
        var m1 = TestData.Materia(1, semestre: 1, horarios: [TestData.Block(DiaSemana.Lunes, 8, 10)]);
        var m2 = TestData.Materia(2, semestre: 2, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var ctx = TestData.Context([m1, m2]);

        BuildEngine().Evaluate(ctx).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MultipleMateriasWithDifferentFailures_ReturnsAllViolationsWithoutShortCircuit()
    {
        // m1: other career + semester exceeded; m2: missing prerequisite; m3: seats exhausted + already approved.
        var m1 = TestData.Materia(1, carreraId: 2, semestre: 9);
        var m2 = TestData.Materia(2, prerequisiteIds: [50]);
        var m3 = TestData.Materia(3, cupos: 1);
        var ctx = TestData.Context(
            [m1, m2, m3],
            approved: [3],
            seatCounts: new Dictionary<int, int> { [3] = 1 });

        var violations = BuildEngine().Evaluate(ctx);

        violations.Select(v => (v.MateriaId, v.Code)).Should().BeEquivalentTo(new[]
        {
            (1, ErrorCodes.CareerMismatch),
            (1, ErrorCodes.SemesterExceeded),
            (2, ErrorCodes.PrerequisiteNotMet),
            (3, ErrorCodes.AlreadyApproved),
            (3, ErrorCodes.SeatsExhausted)
        });
    }

    [Fact]
    public void Evaluate_IntraRequestClash_ReportedOnlyOnLaterCandidate()
    {
        var m1 = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Martes, 10, 12)]);
        var m2 = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Martes, 11, 13)]);
        var ctx = TestData.Context([m1, m2]);

        var violations = BuildEngine().Evaluate(ctx);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.ScheduleOverlap);
        violations[0].MateriaId.Should().Be(2);
    }

    [Fact]
    public void Evaluate_FourthNextSemesterCandidate_ReportsCapOnlyOnThatCandidate()
    {
        // Student in semester 2: semester 3 is N+1, so every candidate is an "extra".
        var candidates = Enumerable.Range(1, 4)
            .Select(i => TestData.Materia(i, semestre: 3, horarios: [TestData.Block(DiaSemana.Lunes, 6 + i * 2, 8 + i * 2)]))
            .ToArray();
        var ctx = TestData.Context(candidates, maxExtraSubjects: 3);

        var violations = BuildEngine().Evaluate(ctx);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        violations[0].MateriaId.Should().Be(4);
    }

    [Fact]
    public void EvaluateCandidate_ReturnsOnlyViolationsOfThatCandidate()
    {
        var ok = TestData.Materia(1, semestre: 2);
        var bad = TestData.Materia(2, semestre: 2, prerequisiteIds: [50]);
        var ctx = TestData.Context([ok, bad]);
        var engine = BuildEngine();

        engine.EvaluateCandidate(ctx, ok).Should().BeEmpty();
        engine.EvaluateCandidate(ctx, bad).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.PrerequisiteNotMet);
    }

    [Fact]
    public void EvaluateCandidate_MatchesEvaluateForEveryCandidate()
    {
        var m1 = TestData.Materia(1, carreraId: 2, semestre: 9);
        var m2 = TestData.Materia(2, prerequisiteIds: [50]);
        var m3 = TestData.Materia(3, cupos: 1);
        var ctx = TestData.Context(
            [m1, m2, m3],
            approved: [3],
            seatCounts: new Dictionary<int, int> { [3] = 1 });
        var engine = BuildEngine();

        var perCandidate = ctx.Candidates.SelectMany(c => engine.EvaluateCandidate(ctx, c)).ToList();

        perCandidate.Should().BeEquivalentTo(engine.Evaluate(ctx));
    }

    [Fact]
    public void Evaluate_NoRules_ReturnsNoViolations()
    {
        var ctx = TestData.Context([TestData.Materia(1)]);

        new EnrollmentRulesEngine([]).Evaluate(ctx).Should().BeEmpty();
    }
}
