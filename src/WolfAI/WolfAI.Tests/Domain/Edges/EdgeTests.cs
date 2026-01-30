using FluentAssertions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Edges;

namespace WolfAI.Tests.Domain.Edges;

public class EdgeTests
{
    private class MockExecutionContext : IExecutionContext
    {
        public string ExecutionId { get; } = "test-exec";
        public string ThreadId { get; } = "test-thread";
        public string GraphId { get; } = "test-graph";
        public string CurrentNodeId { get; set; } = "test-node";
        public IDictionary<string, object?> GlobalVariables { get; } = new Dictionary<string, object?>();
        public VariableScope Variables { get; } = new VariableScope(new Dictionary<string, object?>());
        public List<BaseMessage> Messages { get; } = new();
        public Stack<string> NodeExecutionHistory { get; } = new();
        public IServiceProvider? ServiceProvider { get; }
        public ILogger? Logger { get; }
        public ActivitySource? ActivitySource { get; }
        public Activity? CurrentActivity { get; set; }
        public CancellationToken CancellationToken { get; }
        public ExecutionMetrics Metrics { get; } = new();
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public Dictionary<string, object?> Metadata { get; } = new();
        public TimeSpan Elapsed { get; }

        public bool ContextValue { get; set; }

        public void AddMessage(BaseMessage message) { }
        public void RecordNodeExecution(string nodeId) { }
        public IReadOnlyList<string> GetExecutionHistory() => new List<string>();
    }

    [Fact]
    public void Edge_Constructor_Sets_Properties()
    {
        // Arrange & Act
        var edge = new Edge("edge-1", "node-1", "node-2");

        // Assert
        edge.Id.Should().Be("edge-1");
        edge.SourceNodeId.Should().Be("node-1");
        edge.TargetNodeId.Should().Be("node-2");
        edge.Priority.Should().Be(0);
        edge.RoutingFunction.Should().BeNull();
    }

    [Fact]
    public void Edge_Constructor_Throws_When_Id_Null()
    {
        // Act & Assert
        var act = () => new Edge(null!, "node-1", "node-2");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Edge_Constructor_Throws_When_SourceNodeId_Null()
    {
        // Act & Assert
        var act = () => new Edge("edge-1", null!, "node-2");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Edge_Constructor_Throws_When_TargetNodeId_Null()
    {
        // Act & Assert
        var act = () => new Edge("edge-1", "node-1", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Edge_With_Custom_Priority()
    {
        // Arrange & Act
        var edge = new Edge("edge-1", "node-1", "node-2", priority: 5);

        // Assert
        edge.Priority.Should().Be(5);
    }

    [Fact]
    public void Edge_With_Metadata()
    {
        // Arrange
        var metadata = new EdgeMetadata("Test edge", new[] { "important" });

        // Act
        var edge = new Edge("edge-1", "node-1", "node-2", metadata: metadata);

        // Assert
        edge.Metadata.Description.Should().Be("Test edge");
        edge.Metadata.Tags.Should().Contain("important");
    }

    [Fact]
    public void Edge_Evaluate_Returns_True_When_RoutingFunction_Null()
    {
        // Arrange
        var edge = new Edge("edge-1", "node-1", "node-2", routingFunction: null);
        var context = new MockExecutionContext();

        // Act
        var result = edge.Evaluate(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Edge_Evaluate_Calls_RoutingFunction()
    {
        // Arrange
        var routingCalled = false;
        Func<IExecutionContext, bool> routingFunc = ctx =>
        {
            routingCalled = true;
            return true;
        };
        var edge = new Edge("edge-1", "node-1", "node-2", routingFunction: routingFunc);
        var context = new MockExecutionContext();

        // Act
        var result = edge.Evaluate(context);

        // Assert
        routingCalled.Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public void Edge_Evaluate_Returns_RoutingFunction_Result()
    {
        // Arrange
        var context = new MockExecutionContext { ContextValue = false };
        var edge = new Edge(
            "edge-1",
            "node-1",
            "node-2",
            routingFunction: ctx => ((MockExecutionContext)ctx).ContextValue);

        // Act
        var result = edge.Evaluate(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Edge_Priority_Can_Be_Negative()
    {
        // Arrange & Act
        var edge = new Edge("edge-1", "node-1", "node-2", priority: -10);

        // Assert
        edge.Priority.Should().Be(-10);
    }
}
