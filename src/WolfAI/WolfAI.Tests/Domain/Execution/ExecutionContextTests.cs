using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using ExecutionContextClass = WolfAI.Core.Domain.Execution.ExecutionContext;

namespace WolfAI.Tests.Domain.Execution;

public class ExecutionContextTests
{
    private ExecutionContextClass CreateExecutionContext(
        string? executionId = null,
        string? threadId = null,
        string? graphId = null,
        string? currentNodeId = null,
        IDictionary<string, object?>? globalVariables = null)
    {
        return new ExecutionContextClass(
            executionId ?? "exec-1",
            threadId ?? "thread-1",
            graphId ?? "graph-1",
            currentNodeId ?? "node-1",
            globalVariables);
    }

    [Fact]
    public void ExecutionContext_Constructor_Sets_Properties()
    {
        var globals = new Dictionary<string, object?> { { "var1", "value1" } };

        var context = CreateExecutionContext(
            executionId: "exec-123",
            threadId: "thread-456",
            graphId: "graph-789",
            currentNodeId: "node-1",
            globalVariables: globals);

        context.ExecutionId.Should().Be("exec-123");
        context.ThreadId.Should().Be("thread-456");
        context.GraphId.Should().Be("graph-789");
        context.CurrentNodeId.Should().Be("node-1");
        context.GlobalVariables.Should().Contain("var1", "value1");
    }

    [Fact]
    public void ExecutionContext_Constructor_Throws_When_ExecutionId_Null()
    {
        var act = () => new ExecutionContextClass(null!, "thread-1", "graph-1", "node-1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_Constructor_Throws_When_ThreadId_Null()
    {
        var act = () => new ExecutionContextClass("exec-1", null!, "graph-1", "node-1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_Constructor_Throws_When_GraphId_Null()
    {
        var act = () => new ExecutionContextClass("exec-1", "thread-1", null!, "node-1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_Constructor_Throws_When_CurrentNodeId_Null()
    {
        var act = () => new ExecutionContextClass("exec-1", "thread-1", "graph-1", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_Constructor_Initializes_Collections()
    {
        var context = CreateExecutionContext();

        context.Messages.Should().BeEmpty();
        context.NodeExecutionHistory.Should().BeEmpty();
        context.Metadata.Should().BeEmpty();
        context.Metrics.Should().NotBeNull();
        context.Variables.Should().NotBeNull();
    }

    [Fact]
    public void ExecutionContext_Constructor_Sets_StartedAt()
    {
        var beforeCreation = DateTime.UtcNow;
        var context = CreateExecutionContext();
        var afterCreation = DateTime.UtcNow;

        context.StartedAt.Should().BeOnOrAfter(beforeCreation);
        context.StartedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void ExecutionContext_Elapsed_Returns_Correct_Duration()
    {
        var context = CreateExecutionContext();
        var initialElapsed = context.Elapsed;

        System.Threading.Thread.Sleep(100);

        var laterElapsed = context.Elapsed;

        laterElapsed.Should().BeGreaterThan(initialElapsed);
        laterElapsed.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public void ExecutionContext_CurrentNodeId_Can_Be_Changed()
    {
        var context = CreateExecutionContext();

        context.CurrentNodeId = "node-2";

        context.CurrentNodeId.Should().Be("node-2");
    }

    [Fact]
    public void ExecutionContext_AddMessage_Adds_To_History()
    {
        var context = CreateExecutionContext();
        var message = new HumanMessage("m1", new MessageContent("Hello"));

        context.AddMessage(message);

        context.Messages.Should().HaveCount(1);
        context.Messages[0].Should().Be(message);
    }

    [Fact]
    public void ExecutionContext_AddMessage_Supports_Multiple_Messages()
    {
        var context = CreateExecutionContext();
        var message1 = new HumanMessage("m1", new MessageContent("Hello"));
        var message2 = new AIMessage("m2", new MessageContent("Hi"));

        context.AddMessage(message1);
        context.AddMessage(message2);

        context.Messages.Should().HaveCount(2);
        context.Messages[0].Should().Be(message1);
        context.Messages[1].Should().Be(message2);
    }

    [Fact]
    public void ExecutionContext_AddMessage_Throws_When_Message_Null()
    {
        var context = CreateExecutionContext();

        var act = () => context.AddMessage(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_RecordNodeExecution_Adds_To_History()
    {
        var context = CreateExecutionContext();

        context.RecordNodeExecution("node-1");
        context.RecordNodeExecution("node-2");

        context.NodeExecutionHistory.Should().HaveCount(2);
    }

    [Fact]
    public void ExecutionContext_RecordNodeExecution_Throws_When_NodeId_Null()
    {
        var context = CreateExecutionContext();

        var act = () => context.RecordNodeExecution(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_GetExecutionHistory_Returns_Reverse_Order()
    {
        var context = CreateExecutionContext();
        context.RecordNodeExecution("node-1");
        context.RecordNodeExecution("node-2");
        context.RecordNodeExecution("node-3");

        var history = context.GetExecutionHistory();

        history.Should().HaveCount(3);
        history[0].Should().Be("node-3");
        history[1].Should().Be("node-2");
        history[2].Should().Be("node-1");
    }

    [Fact]
    public void ExecutionContext_Variables_Can_Access_Global()
    {
        var globals = new Dictionary<string, object?> { { "global_key", "global_value" } };
        var context = CreateExecutionContext(globalVariables: globals);

        var value = context.Variables.GetGlobal("global_key");

        value.Should().Be("global_value");
    }

    [Fact]
    public void ExecutionContext_Variables_Can_Set_NodeScoped()
    {
        var context = CreateExecutionContext();

        context.Variables.SetNodeVariable("node-1", "node_key", "node_value");

        var value = context.Variables.GetNodeVariable("node-1", "node_key");
        value.Should().Be("node_value");
    }

    [Fact]
    public void ExecutionContext_Metrics_Are_Available()
    {
        var context = CreateExecutionContext();

        context.Metrics.Should().NotBeNull();
        context.Metrics.NodesExecuted.Should().Be(0);
        context.Metrics.TotalTokensUsed.Should().Be(0);
    }

    [Fact]
    public void ExecutionContext_Metadata_Can_Store_Data()
    {
        var context = CreateExecutionContext();

        context.Metadata["key1"] = "value1";
        context.Metadata["key2"] = 42;

        context.Metadata["key1"].Should().Be("value1");
        context.Metadata["key2"].Should().Be(42);
    }

    [Fact]
    public void ExecutionContext_ServiceProvider_Can_Be_Set()
    {
        var serviceProvider = new Mock<IServiceProvider>().Object;
        var context = new ExecutionContextClass(
            "exec-1",
            "thread-1",
            "graph-1",
            "node-1",
            serviceProvider: serviceProvider);

        context.ServiceProvider.Should().Be(serviceProvider);
    }

    [Fact]
    public void ExecutionContext_Logger_Can_Be_Set()
    {
        var logger = new Mock<ILogger>().Object;
        var context = new ExecutionContextClass(
            "exec-1",
            "thread-1",
            "graph-1",
            "node-1",
            logger: logger);

        context.Logger.Should().Be(logger);
    }

    [Fact]
    public void ExecutionContext_CreateSnapshot_Captures_State()
    {
        var globals = new Dictionary<string, object?> { { "global_key", "global_value" } };
        var context = CreateExecutionContext(globalVariables: globals);
        context.AddMessage(new HumanMessage("m1", new MessageContent("Hello")));
        context.RecordNodeExecution("node-1");
        context.Metrics.NodesExecuted = 5;

        var snapshot = context.CreateSnapshot();

        snapshot.ExecutionId.Should().Be(context.ExecutionId);
        snapshot.ThreadId.Should().Be(context.ThreadId);
        snapshot.GraphId.Should().Be(context.GraphId);
        snapshot.CurrentNodeId.Should().Be(context.CurrentNodeId);
        snapshot.GlobalVariables.Should().Contain("global_key", "global_value");
        snapshot.Messages.Should().HaveCount(1);
        snapshot.NodeExecutionHistory.Should().HaveCount(1);
        snapshot.Metrics.NodesExecuted.Should().Be(5);
    }

    [Fact]
    public void ExecutionContext_RestoreFromSnapshot_Restores_State()
    {
        var globals1 = new Dictionary<string, object?> { { "key1", "value1" } };
        var context1 = CreateExecutionContext(globalVariables: globals1);
        context1.AddMessage(new HumanMessage("m1", new MessageContent("Hello")));
        context1.RecordNodeExecution("node-1");
        context1.Metrics.NodesExecuted = 5;

        var snapshot = context1.CreateSnapshot();

        var globals2 = new Dictionary<string, object?>();
        var context2 = new ExecutionContextClass(
            "exec-2",
            "thread-2",
            "graph-2",
            "node-2",
            globalVariables: globals2);

        context2.RestoreFromSnapshot(snapshot);

        context2.ExecutionId.Should().Be("exec-2"); // ExecutionId not restored
        context2.GlobalVariables.Should().Contain("key1", "value1");
        context2.Messages.Should().HaveCount(1);
        context2.NodeExecutionHistory.Should().HaveCount(1);
        context2.Metrics.NodesExecuted.Should().Be(5);
    }

    [Fact]
    public void ExecutionContext_RestoreFromSnapshot_Throws_When_Snapshot_Null()
    {
        var context = CreateExecutionContext();

        var act = () => context.RestoreFromSnapshot(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecutionContext_RestoreFromSnapshot_Clears_Previous_State()
    {
        var context = CreateExecutionContext();
        context.AddMessage(new HumanMessage("m1", new MessageContent("Hello")));
        context.RecordNodeExecution("node-old");
        context.GlobalVariables["old_key"] = "old_value";

        var snapshot = new ExecutionContextSnapshot
        {
            ExecutionId = "exec-1",
            ThreadId = "thread-1",
            GraphId = "graph-1",
            CurrentNodeId = "node-new",
            GlobalVariables = new Dictionary<string, object?> { { "new_key", "new_value" } },
            Messages = new List<BaseMessage>(),
            NodeExecutionHistory = new Stack<string>(),
            Metrics = new ExecutionMetrics(),
            StartedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object?>()
        };

        context.RestoreFromSnapshot(snapshot);

        context.CurrentNodeId.Should().Be("node-new");
        context.GlobalVariables.Should().NotContainKey("old_key");
        context.GlobalVariables.Should().ContainKey("new_key");
        context.Messages.Should().BeEmpty();
        context.NodeExecutionHistory.Should().BeEmpty();
    }

    [Fact]
    public void ExecutionContext_CancellationToken_Can_Be_Set()
    {
        var cts = new CancellationTokenSource();
        var context = new ExecutionContextClass(
            "exec-1",
            "thread-1",
            "graph-1",
            "node-1",
            cancellationToken: cts.Token);

        context.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void ExecutionContext_GlobalVariables_Map_Accessible()
    {
        var context = CreateExecutionContext();

        context.GlobalVariables["key1"] = "value1";

        context.GlobalVariables["key1"].Should().Be("value1");
    }

    [Fact]
    public void ExecutionContext_CurrentActivity_Can_Be_Set()
    {
        var context = CreateExecutionContext();

        context.CurrentActivity.Should().BeNull();

        var activity = new System.Diagnostics.Activity("test");
        context.CurrentActivity = activity;

        context.CurrentActivity.Should().Be(activity);
    }
}
