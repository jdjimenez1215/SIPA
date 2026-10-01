using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class CareerMatchRuleTests
{
    private readonly CareerMatchRule _rule = new();

    [Fact]
    public void Evaluate_MateriaBelongsToStudentCareer_ReturnsNoViolations()
    {
        var materia = TestData.Materia(1, carreraId: 1);
        var ctx = TestData.Context([materia], TestData.Student(carreraId: 1));

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MateriaFromAnotherCareer_ReturnsCareerMismatch()
    {
        var materia = TestData.Materia(12, carreraId: 2, codigo: "ADM101");
        var ctx = TestData.Context([materia], TestData.Student(carreraId: 1));

        var violations = TestData.Run(_rule, ctx, materia);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.CareerMismatch);
        violations[0].MateriaId.Should().Be(12);
        violations[0].MateriaCodigo.Should().Be("ADM101");
    }
}
