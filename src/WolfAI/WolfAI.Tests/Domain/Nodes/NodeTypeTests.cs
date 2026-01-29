using FluentAssertions;
using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Tests.Domain.Nodes;

public class NodeTypeTests
{
    [Fact]
    public void NodeType_Should_Have_All_Required_Values()
    {
        // Arrange & Act & Assert
        NodeType.Start.Should().Be((NodeType)0);
        NodeType.End.Should().Be((NodeType)1);
        NodeType.AI.Should().Be((NodeType)2);
        NodeType.Tool.Should().Be((NodeType)3);
    }

    [Fact]
    public void NodeType_Should_Be_Convertible_To_String()
    {
        // Arrange & Act
        var startStr = NodeType.Start.ToString();
        var endStr = NodeType.End.ToString();
        var aiStr = NodeType.AI.ToString();
        var toolStr = NodeType.Tool.ToString();

        // Assert
        startStr.Should().Be("Start");
        endStr.Should().Be("End");
        aiStr.Should().Be("AI");
        toolStr.Should().Be("Tool");
    }
}
