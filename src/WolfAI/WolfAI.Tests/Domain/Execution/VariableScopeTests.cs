using FluentAssertions;
using WolfAI.Core.Domain.Execution;

namespace WolfAI.Tests.Domain.Execution;

public class VariableScopeTests
{
    private VariableScope CreateVariableScope(Dictionary<string, object?>? globalVars = null)
    {
        var globals = globalVars ?? new Dictionary<string, object?>();
        return new VariableScope(globals);
    }

    [Fact]
    public void VariableScope_Constructor_Throws_When_GlobalVariables_Null()
    {
        var act = () => new VariableScope(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_GetGlobal_Returns_Existing_Variable()
    {
        var globals = new Dictionary<string, object?> { { "key1", "value1" } };
        var scope = CreateVariableScope(globals);

        var result = scope.GetGlobal("key1");

        result.Should().Be("value1");
    }

    [Fact]
    public void VariableScope_GetGlobal_Throws_When_Key_Not_Found()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetGlobal("nonexistent");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void VariableScope_GetGlobal_Throws_When_Key_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetGlobal(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_SetGlobal_Stores_Variable()
    {
        var globals = new Dictionary<string, object?>();
        var scope = CreateVariableScope(globals);

        scope.SetGlobal("key1", "value1");

        globals["key1"].Should().Be("value1");
    }

    [Fact]
    public void VariableScope_SetGlobal_Updates_Existing_Variable()
    {
        var globals = new Dictionary<string, object?> { { "key1", "oldvalue" } };
        var scope = CreateVariableScope(globals);

        scope.SetGlobal("key1", "newvalue");

        globals["key1"].Should().Be("newvalue");
    }

    [Fact]
    public void VariableScope_SetGlobal_Throws_When_Key_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.SetGlobal(null!, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_GetNodeVariable_Returns_Node_Scoped_Variable()
    {
        var scope = CreateVariableScope();
        scope.SetNodeVariable("node1", "key1", "value1");

        var result = scope.GetNodeVariable("node1", "key1");

        result.Should().Be("value1");
    }

    [Fact]
    public void VariableScope_GetNodeVariable_Throws_When_Node_Not_Found()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetNodeVariable("nonexistent", "key1");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void VariableScope_GetNodeVariable_Throws_When_Key_Not_Found_In_Node()
    {
        var scope = CreateVariableScope();
        scope.SetNodeVariable("node1", "key1", "value1");

        var act = () => scope.GetNodeVariable("node1", "key2");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void VariableScope_GetNodeVariable_Throws_When_NodeId_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetNodeVariable(null!, "key");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_GetNodeVariable_Throws_When_Key_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetNodeVariable("node1", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_SetNodeVariable_Stores_Node_Variable()
    {
        var scope = CreateVariableScope();

        scope.SetNodeVariable("node1", "key1", "value1");

        scope.GetNodeVariable("node1", "key1").Should().Be("value1");
    }

    [Fact]
    public void VariableScope_SetNodeVariable_Multiple_Nodes_Isolated()
    {
        var scope = CreateVariableScope();
        scope.SetNodeVariable("node1", "key", "value1");
        scope.SetNodeVariable("node2", "key", "value2");

        scope.GetNodeVariable("node1", "key").Should().Be("value1");
        scope.GetNodeVariable("node2", "key").Should().Be("value2");
    }

    [Fact]
    public void VariableScope_SetNodeVariable_Throws_When_NodeId_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.SetNodeVariable(null!, "key", "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_SetNodeVariable_Throws_When_Key_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.SetNodeVariable("node1", null!, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_TryGetVariable_Returns_NodeScoped_When_NodeId_Provided()
    {
        var globals = new Dictionary<string, object?> { { "key", "global_value" } };
        var scope = CreateVariableScope(globals);
        scope.SetNodeVariable("node1", "key", "node_value");

        var result = scope.TryGetVariable("key", out var value, "node1");

        result.Should().BeTrue();
        value.Should().Be("node_value");
    }

    [Fact]
    public void VariableScope_TryGetVariable_Falls_Back_To_Global()
    {
        var globals = new Dictionary<string, object?> { { "key", "global_value" } };
        var scope = CreateVariableScope(globals);

        var result = scope.TryGetVariable("key", out var value, "node1");

        result.Should().BeTrue();
        value.Should().Be("global_value");
    }

    [Fact]
    public void VariableScope_TryGetVariable_Falls_Back_To_Global_When_NodeId_Not_Provided()
    {
        var globals = new Dictionary<string, object?> { { "key", "global_value" } };
        var scope = CreateVariableScope(globals);

        var result = scope.TryGetVariable("key", out var value);

        result.Should().BeTrue();
        value.Should().Be("global_value");
    }

    [Fact]
    public void VariableScope_TryGetVariable_Returns_False_When_Not_Found()
    {
        var scope = CreateVariableScope();

        var result = scope.TryGetVariable("nonexistent", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void VariableScope_TryGetVariable_Throws_When_Key_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.TryGetVariable(null!, out _);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_GetAllGlobalVariables_Returns_All_Globals()
    {
        var globals = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var scope = CreateVariableScope(globals);

        var result = scope.GetAllGlobalVariables();

        result.Should().HaveCount(2);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
    }

    [Fact]
    public void VariableScope_GetAllNodeVariables_Returns_Node_Variables()
    {
        var scope = CreateVariableScope();
        scope.SetNodeVariable("node1", "key1", "value1");
        scope.SetNodeVariable("node1", "key2", "value2");

        var result = scope.GetAllNodeVariables("node1");

        result.Should().HaveCount(2);
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
    }

    [Fact]
    public void VariableScope_GetAllNodeVariables_Returns_Empty_When_Node_Not_Found()
    {
        var scope = CreateVariableScope();

        var result = scope.GetAllNodeVariables("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public void VariableScope_GetAllNodeVariables_Throws_When_NodeId_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.GetAllNodeVariables(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_ClearNodeVariables_Removes_All_Node_Variables()
    {
        var scope = CreateVariableScope();
        scope.SetNodeVariable("node1", "key1", "value1");
        scope.SetNodeVariable("node1", "key2", "value2");

        scope.ClearNodeVariables("node1");

        scope.GetAllNodeVariables("node1").Should().BeEmpty();
    }

    [Fact]
    public void VariableScope_ClearNodeVariables_Throws_When_NodeId_Null()
    {
        var scope = CreateVariableScope();

        var act = () => scope.ClearNodeVariables(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VariableScope_Supports_Concurrent_Access()
    {
        var scope = CreateVariableScope();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            int nodeIndex = i;
            tasks.Add(Task.Run(() =>
            {
                scope.SetNodeVariable($"node{nodeIndex}", "key", $"value{nodeIndex}");
            }));
        }

        Task.WaitAll(tasks.ToArray());

        for (int i = 0; i < 100; i++)
        {
            scope.GetNodeVariable($"node{i}", "key").Should().Be($"value{i}");
        }
    }

    [Fact]
    public void VariableScope_Supports_Concurrent_Global_Updates()
    {
        var globals = new Dictionary<string, object?>();
        var scope = CreateVariableScope(globals);
        var lockObj = new object();

        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() =>
            {
                lock (lockObj)
                {
                    scope.SetGlobal($"key{i}", $"value{i}");
                }
            })
        ).ToList();

        Task.WaitAll(tasks.ToArray());

        globals.Should().HaveCount(100);
    }

    [Fact]
    public void VariableScope_SetNodeVariable_Supports_Null_Values()
    {
        var scope = CreateVariableScope();

        scope.SetNodeVariable("node1", "key", null);

        scope.GetNodeVariable("node1", "key").Should().BeNull();
    }

    [Fact]
    public void VariableScope_SetGlobal_Supports_Null_Values()
    {
        var globals = new Dictionary<string, object?>();
        var scope = CreateVariableScope(globals);

        scope.SetGlobal("key", null);

        globals["key"].Should().BeNull();
    }
}
