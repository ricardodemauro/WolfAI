using FluentAssertions;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Tests.Domain.Execution;

public class ExecutionMetricsTests
{
    [Fact]
    public void ExecutionMetrics_Initialize_Sets_Default_Values()
    {
        var metrics = new ExecutionMetrics();

        metrics.NodesExecuted.Should().Be(0);
        metrics.TotalTokensUsed.Should().Be(0);
        metrics.TotalEstimatedCost.Should().Be(0m);
        metrics.TotalDuration.Should().Be(TimeSpan.Zero);
        metrics.NodeTypeCounters.Should().HaveCount(4);
    }

    [Fact]
    public void ExecutionMetrics_Initialize_Creates_All_NodeType_Counters()
    {
        var metrics = new ExecutionMetrics();

        metrics.NodeTypeCounters.Should().ContainKey(NodeType.Start);
        metrics.NodeTypeCounters.Should().ContainKey(NodeType.End);
        metrics.NodeTypeCounters.Should().ContainKey(NodeType.AI);
        metrics.NodeTypeCounters.Should().ContainKey(NodeType.Tool);
    }

    [Fact]
    public void ExecutionMetrics_IncrementNodeTypeCounter_Increases_Counter()
    {
        var metrics = new ExecutionMetrics();

        metrics.IncrementNodeTypeCounter(NodeType.AI);
        metrics.IncrementNodeTypeCounter(NodeType.AI);
        metrics.IncrementNodeTypeCounter(NodeType.Start);

        metrics.NodeTypeCounters[NodeType.AI].Should().Be(2);
        metrics.NodeTypeCounters[NodeType.Start].Should().Be(1);
        metrics.NodeTypeCounters[NodeType.End].Should().Be(0);
        metrics.NodeTypeCounters[NodeType.Tool].Should().Be(0);
    }

    [Fact]
    public void ExecutionMetrics_AddTokens_Accumulates_Tokens()
    {
        var metrics = new ExecutionMetrics();

        metrics.AddTokens(100);
        metrics.AddTokens(50);
        metrics.AddTokens(25);

        metrics.TotalTokensUsed.Should().Be(175);
    }

    [Fact]
    public void ExecutionMetrics_AddCost_Accumulates_Cost()
    {
        var metrics = new ExecutionMetrics();

        metrics.AddCost(1.50m);
        metrics.AddCost(0.75m);
        metrics.AddCost(0.25m);

        metrics.TotalEstimatedCost.Should().Be(2.50m);
    }

    [Fact]
    public void ExecutionMetrics_NodesExecuted_Can_Be_Set()
    {
        var metrics = new ExecutionMetrics();

        metrics.NodesExecuted = 5;

        metrics.NodesExecuted.Should().Be(5);
    }

    [Fact]
    public void ExecutionMetrics_TotalDuration_Can_Be_Set()
    {
        var metrics = new ExecutionMetrics();
        var duration = TimeSpan.FromSeconds(42);

        metrics.TotalDuration = duration;

        metrics.TotalDuration.Should().Be(duration);
    }
}
