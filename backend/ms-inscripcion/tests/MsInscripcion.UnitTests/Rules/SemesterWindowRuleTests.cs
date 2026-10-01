using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class SemesterWindowRuleTests
{
    private readonly SemesterWindowRule _rule = new();

    [Fact]
    public void Evaluate_CurrentSemester_ReturnsNoViolations()
    {
        var materia = TestData.Materia(1, semestre: 5);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 5));

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_OwedSubjectBelowCurrentSemester_ReturnsNoViolations()
    {
        var materia = TestData.Materia(2, semestre: 2);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 5));

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_NextSemester_ReturnsNoViolations()
    {
        var materia = TestData.Materia(3, semestre: 6);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 5));

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_TwoSemestersAhead_ReturnsSemesterExceededWithDetails()
    {
        var materia = TestData.Materia(4, semestre: 7);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 5));

        var violations = TestData.Run(_rule, ctx, materia);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.SemesterExceeded);
        violations[0].MateriaId.Should().Be(4);
        violations[0].Details!["materiaSemestre"].Should().Be(7);
        violations[0].Details!["maxAllowedSemestre"].Should().Be(6);
    }

    [Fact]
    public void Evaluate_WindowIgnoresConfiguredCap()
    {
        // The window is fixed at N+1; the cap is a different rule.
        var materia = TestData.Materia(5, semestre: 4);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 2), maxExtraSubjects: 0);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle()
            .Which.Details!["maxAllowedSemestre"].Should().Be(3);
    }
}
