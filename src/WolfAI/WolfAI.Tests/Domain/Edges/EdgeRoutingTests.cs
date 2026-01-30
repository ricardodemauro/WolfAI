using FluentAssertions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Tests.Fixtures;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Edges;
using GraphModel = WolfAI.Core.Domain.Graph.Graph;

namespace WolfAI.Tests.Domain.Edges;

public class EdgeRoutingTests
{
    private class MockExecutionContext : IExecutionContext
    {
        public string ExecutionId { get; } = "test-exec";
        public string ThreadId { get; } = "test-thread";
        public string GraphId { get; } = "test-graph";
        public string CurrentNodeId { get; set; } = "test-node";
        public IReadOnlyDictionary<string, object?> GlobalVariables { get; } = new Dictionary<string, object?>();
        public VariableScope Variables { get; } = new VariableScope(new Dictionary<string, object?>());
        public IReadOnlyList<BaseMessage> Messages { get; } = new List<BaseMessage>();
        public IReadOnlyList<string> NodeExecutionHistory { get; } = new List<string>();
        public IServiceProvider? ServiceProvider { get; }
        public ILogger? Logger { get; }
        public ActivitySource? ActivitySource { get; }
        public Activity? CurrentActivity { get; set; }
        public CancellationToken CancellationToken { get; }
        public ExecutionMetrics Metrics { get; } = new();
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public IReadOnlyDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
        public TimeSpan Elapsed { get; }

        public int NodesExecuted { get; set; }
        public string? LastNodeId { get; set; }
    }

    [Fact]
    public void Edge_Routing_With_Multiple_Edges_By_Priority()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "branch1", new TestNode("branch1", "Branch1") },
            { "branch2", new TestNode("branch2", "Branch2") },
        };

        var context = new MockExecutionContext { NodesExecuted = 5 };

        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "branch1", ctx => ((MockExecutionContext)ctx).NodesExecuted > 10, priority: 0),
            new Edge("edge-2", "start", "branch2", ctx => ((MockExecutionContext)ctx).NodesExecuted <= 10, priority: 1)
        };

        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var outgoing = graph.GetOutgoingEdges("start");
        var validEdges = outgoing.Where(e => e.Evaluate(context)).ToList();

        // Assert
        validEdges.Should().HaveCount(1);
        validEdges[0].TargetNodeId.Should().Be("branch2");
    }

    [Fact]
    public void Edge_Routing_Returns_First_Valid_Edge_By_Priority()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "option1", new TestNode("option1", "Option1") },
            { "option2", new TestNode("option2", "Option2") },
            { "option3", new TestNode("option3", "Option3") }
        };

        var context = new MockExecutionContext();

        // Both option1 and option2 routing functions return true, option3 returns false
        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "option1", ctx => true, priority: 0),
            new Edge("edge-2", "start", "option2", ctx => true, priority: 1),
            new Edge("edge-3", "start", "option3", ctx => false, priority: -1)
        };

        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var outgoing = graph.GetOutgoingEdges("start");
        var firstValid = outgoing.First(e => e.Evaluate(context));

        // Assert
        firstValid.TargetNodeId.Should().Be("option1");
    }

    [Fact]
    public void Edge_Routing_With_Complex_Logic()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "success", new TestNode("success", "Success") },
            { "retry", new TestNode("retry", "Retry") },
            { "error", new TestNode("error", "Error") }
        };

        var context = new MockExecutionContext { NodesExecuted = 3, LastNodeId = "intermediate" };

        var edges = new List<Edge>
        {
            new Edge("edge-success", "start", "success", 
                ctx => ((MockExecutionContext)ctx).NodesExecuted < 2, priority: 10),
            new Edge("edge-retry", "start", "retry", 
                ctx => ((MockExecutionContext)ctx).NodesExecuted >= 2 && ((MockExecutionContext)ctx).NodesExecuted < 5, 
                priority: 5),
            new Edge("edge-error", "start", "error", 
                ctx => ((MockExecutionContext)ctx).NodesExecuted >= 5, priority: 1)
        };

        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var outgoing = graph.GetOutgoingEdges("start");
        var nextEdge = outgoing.FirstOrDefault(e => e.Evaluate(context));

        // Assert
        nextEdge.Should().NotBeNull();
        nextEdge!.TargetNodeId.Should().Be("retry");
    }
}
