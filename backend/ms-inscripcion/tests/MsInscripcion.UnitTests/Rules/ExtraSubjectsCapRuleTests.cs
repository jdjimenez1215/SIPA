using FluentAssertions;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class ExtraSubjectsCapRuleTests
{
    private readonly ExtraSubjectsCapRule _rule = new();

    // Student in semester 4: N = 4, owed = semestre < 4, N+1 = 5.
    private static Estudiante Student() => TestData.Student(semestreActual: 4);

    [Fact]
    public void Evaluate_ThreeExtrasInRequest_ReturnsNoViolations()
    {
        var candidates = Enumerable.Range(1, 3).Select(i => TestData.Materia(i, semestre: 5)).ToArray();
        var ctx = TestData.Context(candidates, Student(), maxExtraSubjects: 3);

        foreach (var candidate in candidates)
            TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_FourthExtraInRequest_ReturnsCapExceededWithDetails()
    {
        var candidates = Enumerable.Range(1, 4).Select(i => TestData.Materia(i, semestre: 5)).ToArray();
        var ctx = TestData.Context(candidates, Student(), maxExtraSubjects: 3);

        var violations = TestData.Run(_rule, ctx, candidates[3]);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        violations[0].MateriaId.Should().Be(4);
        violations[0].Details!["maxExtraSubjects"].Should().Be(3);
        violations[0].Details!["used"].Should().Be(3);
    }

    [Fact]
    public void Evaluate_ActiveExtrasCountTowardsTheCap()
    {
        var active = new[] { TestData.Materia(10, semestre: 5), TestData.Materia(11, semestre: 5) };
        var candidates = new[] { TestData.Materia(1, semestre: 5), TestData.Materia(2, semestre: 5) };
        var ctx = TestData.Context(candidates, Student(), activeEnrolled: active, maxExtraSubjects: 3);

        TestData.Run(_rule, ctx, candidates[0]).Should().BeEmpty();
        TestData.Run(_rule, ctx, candidates[1]).Should().ContainSingle()
            .Which.Details!["used"].Should().Be(3);
    }

    [Fact]
    public void Evaluate_OwedSubjectsCountTowardsTheCap()
    {
        var candidates = new[]
        {
            TestData.Materia(1, semestre: 2),   // owed
            TestData.Materia(2, semestre: 3),   // owed
            TestData.Materia(3, semestre: 5),   // N+1
            TestData.Materia(4, semestre: 5)    // N+1 -> 4th extra
        };
        var ctx = TestData.Context(candidates, Student(), maxExtraSubjects: 3);

        TestData.Run(_rule, ctx, candidates[2]).Should().BeEmpty();
        TestData.Run(_rule, ctx, candidates[3]).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }

    [Fact]
    public void Evaluate_CurrentSemesterSubjectsAreNotCountedNorLimited()
    {
        var current = Enumerable.Range(1, 5).Select(i => TestData.Materia(i, semestre: 4)).ToArray();
        var extra = TestData.Materia(20, semestre: 5);
        var ctx = TestData.Context([.. current, extra], Student(), maxExtraSubjects: 1);

        foreach (var materia in current)
            TestData.Run(_rule, ctx, materia).Should().BeEmpty();

        TestData.Run(_rule, ctx, extra).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ActiveCurrentSemesterSubjectIsNotCounted()
    {
        var active = new[] { TestData.Materia(10, semestre: 4), TestData.Materia(11, semestre: 4) };
        var candidate = TestData.Materia(1, semestre: 5);
        var ctx = TestData.Context([candidate], Student(), activeEnrolled: active, maxExtraSubjects: 1);

        TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ApprovedOrOtherCareerSubjectIsNotAnExtra()
    {
        var approved = TestData.Materia(1, semestre: 2);
        var otherCareer = TestData.Materia(2, carreraId: 2, semestre: 5);
        var ctx = TestData.Context([approved, otherCareer], Student(), approved: [1], maxExtraSubjects: 0);

        TestData.Run(_rule, ctx, approved).Should().BeEmpty();
        TestData.Run(_rule, ctx, otherCareer).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_CapOfZero_RejectsAnyExtra()
    {
        var candidate = TestData.Materia(1, semestre: 5);
        var ctx = TestData.Context([candidate], Student(), maxExtraSubjects: 0);

        TestData.Run(_rule, ctx, candidate).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }
}
