using FluentAssertions;
using WolfAI.Tests.Fixtures;
using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Tests.Domain.Nodes;

public class NodeTests
{
    [Fact]
    public void Node_Constructor_Sets_Properties()
    {
        // Arrange & Act
        var node = new TestNode("node-1", "Test Node", NodeType.AI);

        // Assert
        node.Id.Should().Be("node-1");
        node.Name.Should().Be("Test Node");
        node.NodeType.Should().Be(NodeType.AI);
    }

    [Fact]
    public void Node_Constructor_Throws_When_Id_Null()
    {
        // Act & Assert
        var act = () => new TestNode(null!, "Test Node");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Node_Constructor_Throws_When_Name_Null()
    {
        // Act & Assert
        var act = () => new TestNode("node-1", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Node_Middleware_Collection_Is_Initialized_Empty()
    {
        // Arrange & Act
        var node = new TestNode("node-1", "Test Node");

        // Assert
        node.Middleware.Should().BeEmpty();
    }

    [Fact]
    public void Node_Supports_Different_NodeTypes()
    {
        // Arrange & Act
        var startNode = new TestNode("start", "Start", NodeType.Start);
        var endNode = new TestNode("end", "End", NodeType.End);
        var aiNode = new TestNode("ai", "AI", NodeType.AI);
        var toolNode = new TestNode("tool", "Tool", NodeType.Tool);

        // Assert
        startNode.NodeType.Should().Be(NodeType.Start);
        endNode.NodeType.Should().Be(NodeType.End);
        aiNode.NodeType.Should().Be(NodeType.AI);
        toolNode.NodeType.Should().Be(NodeType.Tool);
    }

    [Fact]
    public async Task Node_ExecuteAsync_Can_Be_Implemented()
    {
        // Arrange
        var executedLogic = false;
        var node = new TestNode(
            "node-1",
            "Test Node",
            executeLogic: async (ctx, ct) =>
            {
                executedLogic = true;
                await Task.CompletedTask;
                return global::WolfAI.Core.Domain.Results.NodeResult.SuccessResult(output: "executed");
            });

        // Act
        var result = await node.ExecuteAsync(null!, CancellationToken.None);

        // Assert
        executedLogic.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.Output.Should().Be("executed");
    }
}
