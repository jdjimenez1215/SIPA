using FluentAssertions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Rules;

public class SeatAvailabilityRuleTests
{
    private readonly SeatAvailabilityRule _rule = new();

    [Fact]
    public void Evaluate_SeatsRemaining_ReturnsNoViolations()
    {
        var materia = TestData.Materia(1, cupos: 5);
        var ctx = TestData.Context([materia], seatCounts: new Dictionary<int, int> { [1] = 4 });

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ActiveCountEqualsCapacity_ReturnsSeatsExhausted()
    {
        var materia = TestData.Materia(9, cupos: 1, codigo: "ALG201");
        var ctx = TestData.Context([materia], seatCounts: new Dictionary<int, int> { [9] = 1 });

        var violations = TestData.Run(_rule, ctx, materia);

        violations.Should().ContainSingle();
        violations[0].Code.Should().Be(ErrorCodes.SeatsExhausted);
        violations[0].Details!["cuposMaximos"].Should().Be(1);
        violations[0].Details!["inscritos"].Should().Be(1);
    }

    [Fact]
    public void Evaluate_MateriaAbsentFromCounts_TreatedAsZeroEnrolled()
    {
        var materia = TestData.Materia(1, cupos: 1);
        var ctx = TestData.Context([materia]);

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_OnlyCancelledEnrollments_DoNotConsumeSeats()
    {
        // Counts only include Activa enrollments, so a cancelled one leaves the seat free.
        var materia = TestData.Materia(1, cupos: 1);
        var ctx = TestData.Context([materia], seatCounts: new Dictionary<int, int> { [1] = 0 });

        TestData.Run(_rule, ctx, materia).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ZeroCapacity_ReturnsSeatsExhausted()
    {
        var materia = TestData.Materia(1, cupos: 0);
        var ctx = TestData.Context([materia]);

        TestData.Run(_rule, ctx, materia).Should().ContainSingle();
    }
}
