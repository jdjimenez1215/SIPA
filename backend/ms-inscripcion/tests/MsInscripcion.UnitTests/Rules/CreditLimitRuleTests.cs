using FluentAssertions;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Rules;

namespace MsInscripcion.UnitTests.Rules;

public class CreditLimitRuleTests
{
    private static Materia Subject(int id, int credits) => new()
    {
        Id = id,
        Codigo = $"M{id}",
        Nombre = $"Materia {id}",
        Creditos = credits,
        Semestre = 6,
        CarreraId = 1
    };

    private static EnrollmentContext Context(decimal average, IReadOnlyList<Materia> active, Materia candidate) =>
        new(
            new Estudiante { Id = 1, Nombre = "Demo", CarreraId = 1, SemestreActual = 6 },
            "2026-2",
            3,
            new HashSet<int>(),
            active,
            new Dictionary<int, int>(),
            [candidate],
            average);

    [Fact]
    public void AllowsUpToEighteenCreditsWithoutHighAverage()
    {
        var candidate = Subject(1, 18);

        new CreditLimitRule().Evaluate(Context(3.9m, [], candidate), candidate)
            .Should().BeEmpty();
    }

    [Fact]
    public void BlocksNineteenCreditsWithoutHighAverage()
    {
        var active = Subject(2, 2);
        var candidate = Subject(1, 17);

        var violations = new CreditLimitRule().Evaluate(Context(3.9m, [active], candidate), candidate).ToList();

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.CreditLimitExceeded);
        violations[0].Details!["maxCredits"].Should().Be(18);
    }

    [Fact]
    public void AllowsTwentyCreditsWhenAverageIsAboveFour()
    {
        var active = Subject(2, 2);
        var candidate = Subject(1, 18);

        new CreditLimitRule().Evaluate(Context(4.5m, [active], candidate), candidate)
            .Should().BeEmpty();
    }

    [Fact]
    public void ExactlyFourUnlocksTwentyCredits()
    {
        var active = Subject(2, 2);
        var candidate = Subject(1, 18);

        new CreditLimitRule().Evaluate(Context(4.0m, [active], candidate), candidate)
            .Should().BeEmpty();
    }

    [Fact]
    public void BelowFourBlocksMoreThanEighteen()
    {
        var active = Subject(2, 2);
        var candidate = Subject(1, 17);

        new CreditLimitRule().Evaluate(Context(3.99m, [active], candidate), candidate)
            .Should().ContainSingle(v => v.Code == ErrorCodes.CreditLimitExceeded);
    }
}
