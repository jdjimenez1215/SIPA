using FluentAssertions;
using MsInscripcion.Domain.Exceptions;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.UnitTests.Services;

public class PrerequisiteGraphTests
{
    [Fact]
    public void WouldCreateCycle_SelfReference_ReturnsTrue()
    {
        var graph = new PrerequisiteGraph([]);

        graph.WouldCreateCycle(1, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_DirectReverseEdge_ReturnsTrue()
    {
        var graph = new PrerequisiteGraph([(1, 2)]); // 1 requires 2

        graph.WouldCreateCycle(2, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_IndirectCycle_ReturnsTrue()
    {
        var graph = new PrerequisiteGraph([(1, 2), (2, 3)]); // 1 -> 2 -> 3

        graph.WouldCreateCycle(3, 1).Should().BeTrue();
    }

    [Fact]
    public void WouldCreateCycle_ValidEdge_ReturnsFalse()
    {
        var graph = new PrerequisiteGraph([(1, 2), (2, 3)]);

        graph.WouldCreateCycle(1, 3).Should().BeFalse();
    }

    [Fact]
    public void WouldCreateCycle_DiamondShapeWithoutCycle_ReturnsFalse()
    {
        var graph = new PrerequisiteGraph([(1, 2), (1, 3), (2, 4), (3, 4)]);

        graph.WouldCreateCycle(5, 1).Should().BeFalse();
    }

    [Fact]
    public void WouldCreateCycle_ExistingCycleInUnrelatedPart_TerminatesAndReturnsFalse()
    {
        var graph = new PrerequisiteGraph([(10, 11), (11, 10)]);

        graph.WouldCreateCycle(1, 10).Should().BeFalse();
    }

    [Fact]
    public void EnsureNoCycle_CycleDetected_ThrowsDomainExceptionWithCycleCode()
    {
        var graph = new PrerequisiteGraph([(1, 2)]);

        var act = () => graph.EnsureNoCycle(2, 1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(ErrorCodes.PrerequisiteCycle);
    }

    [Fact]
    public void EnsureNoCycle_ValidEdge_DoesNotThrow()
    {
        var graph = new PrerequisiteGraph([(1, 2)]);

        var act = () => graph.EnsureNoCycle(3, 1);

        act.Should().NotThrow();
    }
}
