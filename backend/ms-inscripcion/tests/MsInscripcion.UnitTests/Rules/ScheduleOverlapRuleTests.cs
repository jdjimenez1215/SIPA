using FluentAssertions;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class ScheduleOverlapRuleTests
{
    private readonly ScheduleOverlapRule _rule = new();

    [Fact]
    public void Evaluate_OverlappingWithActiveEnrollment_ReturnsClashWithConflictingCodigo()
    {
        var existing = TestData.Materia(1, codigo: "ETI101", horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var candidate = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Lunes, 11, 13)]);
        var ctx = TestData.Context([candidate], activeEnrolled: [existing]);

        var violations = TestData.Run(_rule, ctx, candidate);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.ScheduleOverlap);
        violations[0].MateriaId.Should().Be(2);
        violations[0].Details!["conflictsWith"].Should().Be("ETI101");
        violations[0].Details!["conflictsWithMateriaId"].Should().Be(1);
    }

    [Fact]
    public void Evaluate_BackToBackBlocks_ReturnsNoViolations()
    {
        var existing = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var candidate = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Lunes, 12, 14)]);
        var ctx = TestData.Context([candidate], activeEnrolled: [existing]);

        TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_SameHoursOnDifferentDay_ReturnsNoViolations()
    {
        var existing = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var candidate = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Martes, 10, 12)]);
        var ctx = TestData.Context([candidate], activeEnrolled: [existing]);

        TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ClashBetweenTwoCandidatesOfSameRequest_ReportedOnLaterCandidateOnly()
    {
        var first = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var second = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Lunes, 11, 13)]);
        var ctx = TestData.Context([first, second]);

        TestData.Run(_rule, ctx, first).Should().BeEmpty();

        var violations = TestData.Run(_rule, ctx, second);
        violations.Should().ContainSingle();
        violations[0].MateriaId.Should().Be(2);
        violations[0].Details!["conflictsWithMateriaId"].Should().Be(1);
    }

    [Fact]
    public void Evaluate_SameMateriaAlreadyEnrolled_DoesNotReportClash()
    {
        // Same id already enrolled is reported only by NoDuplicateRule, never as a schedule clash with itself.
        var enrolled = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var candidate = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var ctx = TestData.Context([candidate], activeEnrolled: [enrolled]);

        TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ClashesWithTwoOthers_ReturnsOneViolationPerConflictingMateria()
    {
        var a = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var b = TestData.Materia(2, horarios: [TestData.Block(DiaSemana.Lunes, 12, 14)]);
        var candidate = TestData.Materia(3, horarios: [TestData.Block(DiaSemana.Lunes, 11, 13)]);
        var ctx = TestData.Context([candidate], activeEnrolled: [a, b]);

        TestData.Run(_rule, ctx, candidate).Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_CandidateWithoutSchedule_ReturnsNoViolations()
    {
        var existing = TestData.Materia(1, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]);
        var candidate = TestData.Materia(2);
        var ctx = TestData.Context([candidate], activeEnrolled: [existing]);

        TestData.Run(_rule, ctx, candidate).Should().BeEmpty();
    }
}
