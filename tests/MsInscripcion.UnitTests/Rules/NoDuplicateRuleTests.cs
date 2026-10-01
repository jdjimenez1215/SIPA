using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class NoDuplicateRuleTests
{
    private readonly NoDuplicateRule _rule = new();

    [Fact]
    public void Evaluate_NeverTakenBefore_ReturnsNoViolations()
    {
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia]);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AlreadyApproved_ReturnsAlreadyApproved()
    {
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia], approved: [1]);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.AlreadyApproved);
    }

    [Fact]
    public void Evaluate_AlreadyActivelyEnrolledInPeriod_ReturnsAlreadyEnrolled()
    {
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia], activeEnrolled: [TestData.Materia(1)]);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.AlreadyEnrolled);
    }

    [Fact]
    public void Evaluate_ApprovedAndActivelyEnrolled_ReturnsBothViolations()
    {
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia], approved: [1], activeEnrolled: [TestData.Materia(1)]);

        TestData.Run(_rule, ctx, materia).Select(v => v.Code)
            .Should().BeEquivalentTo(ErrorCodes.AlreadyApproved, ErrorCodes.AlreadyEnrolled);
    }

    [Fact]
    public void Evaluate_PreviouslyFailedOrCancelled_AllowsReEnroll()
    {
        // Reprobada is not in the approved set and Cancelada is not among the active enrollments.
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia], approved: [2, 3], activeEnrolled: [TestData.Materia(2)]);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }
}
