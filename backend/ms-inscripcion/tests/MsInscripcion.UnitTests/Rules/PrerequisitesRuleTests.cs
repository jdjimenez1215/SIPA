using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class PrerequisitesRuleTests
{
    private readonly PrerequisitesRule _rule = new();

    [Fact]
    public void Evaluate_NoPrerequisites_ReturnsNoViolations()
    {
        var materia = TestData.Materia(1);
        var ctx = TestData.Context([materia]);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AllPrerequisitesApproved_ReturnsNoViolations()
    {
        var materia = TestData.Materia(7, prerequisiteIds: [3, 6]);
        var ctx = TestData.Context([materia], approved: [3, 6, 99]);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_PrerequisiteMissing_ReturnsViolationListingMissingIds()
    {
        var materia = TestData.Materia(7, prerequisiteIds: [3, 6]);
        var ctx = TestData.Context([materia], approved: [3]);

        var violations = TestData.Run(_rule, ctx, materia);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.PrerequisiteNotMet);
        ((IEnumerable<int>)violations[0].Details!["missingIds"]).Should().BeEquivalentTo(new[] { 6 });
    }

    [Fact]
    public void Evaluate_PrerequisiteNavigationLoaded_ReportsRequisitoCodigo()
    {
        var requisito = TestData.Materia(6, codigo: "MAT201");
        var materia = TestData.Materia(7, prerequisiteIds: [6]);
        materia.Prerrequisitos.Single().MateriaRequisito = requisito;
        var ctx = TestData.Context([materia]);

        var violations = TestData.Run(_rule, ctx, materia);

        ((IEnumerable<string>)violations[0].Details!["missing"]).Should().BeEquivalentTo(new[] { "MAT201" });
        violations[0].Message.Should().Contain("MAT201");
    }

    [Fact]
    public void Evaluate_PrerequisiteOnlyInProgress_ReturnsViolation()
    {
        // "Cursando" history entries are never part of ApprovedMateriaIds, so they must not satisfy the prerequisite.
        var materia = TestData.Materia(7, prerequisiteIds: [3]);
        var ctx = TestData.Context([materia], approved: []);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle()
            .Which.Code.Should().Be(ErrorCodes.PrerequisiteNotMet);
    }

    [Fact]
    public void Evaluate_PrerequisiteFailedThenNotApproved_ReturnsViolation()
    {
        // "Reprobada" is not approved either: the prerequisite stays unmet.
        var materia = TestData.Materia(7, prerequisiteIds: [3]);
        var ctx = TestData.Context([materia], approved: [1, 2]);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle();
    }
}
