using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class SemesterLimitRuleTests
{
    private readonly SemesterLimitRule _rule = new();

    [Fact]
    public void Evaluate_SemesterEqualToLimit_ReturnsNoViolations()
    {
        // SemestreActual=2 + MaxSemesterAhead=3 => 5 is the last allowed semester.
        var materia = TestData.Materia(1, semestre: 5);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 2), maxSemesterAhead: 3);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_SemesterAboveLimit_ReturnsSemesterExceededWithDetails()
    {
        var materia = TestData.Materia(2, semestre: 6);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 2), maxSemesterAhead: 3);

        var violations = TestData.Run(_rule, ctx, materia);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.SemesterExceeded);
        violations[0].Details!["materiaSemestre"].Should().Be(6);
        violations[0].Details!["maxAllowedSemestre"].Should().Be(5);
    }

    [Fact]
    public void Evaluate_ConfiguredLimitIsLower_AppliesConfiguredValue()
    {
        var materia = TestData.Materia(3, semestre: 4);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 2), maxSemesterAhead: 1);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle()
            .Which.Details!["maxAllowedSemestre"].Should().Be(3);
    }

    [Fact]
    public void Evaluate_SemesterBelowCurrent_ReturnsNoViolations()
    {
        var materia = TestData.Materia(4, semestre: 1);
        var ctx = TestData.Context([materia], TestData.Student(semestreActual: 2));

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }
}
