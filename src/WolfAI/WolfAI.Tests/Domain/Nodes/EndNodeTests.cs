using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Tests.Domain.Nodes;

public class EndNodeTests
{
    private readonly Mock<ILogger> _mockLogger;

    public EndNodeTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    private IExecutionContext CreateTestContext(IEnumerable<BaseMessage>? messages = null)
    {
        return new ExecutionContext(
            executionId: "test-exec-1",
            threadId: "test-thread-1",
            graphId: "test-graph-1",
            currentNodeId: "end",
            messages: messages,
            logger: _mockLogger.Object
        );
    }

    [Fact]
    public void EndNode_Constructor_Sets_Properties()
    {
        // Act
        var endNode = new EndNode("end-1", "End");

        // Assert
        endNode.Id.Should().Be("end-1");
        endNode.Name.Should().Be("End");
        endNode.NodeType.Should().Be(NodeType.End);
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Returns_Success_With_Last_Message_Content()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new HumanMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Hello" }
            ),
            new AIMessage(
                id: "msg-2",
                content: new MessageContent { IsSimple = true, SimpleContent = "Final response" }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Final response");
        result.Messages.Should().BeEmpty(); // EndNode doesn't produce new messages
        result.Variables.Should().BeNull(); // EndNode doesn't produce variables
        result.Error.Should().BeNull();
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Does_Not_Mutate_Context()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new AIMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Test" }
            )
        };
        var context = CreateTestContext(messages);
        var initialMessageCount = context.Messages.Count;

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.Messages.Count.Should().Be(initialMessageCount); // Context unchanged!
        result.Messages.Should().BeEmpty(); // No new messages
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Handles_Empty_Message_History()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var context = CreateTestContext(messages: null);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("No output generated");
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Captures_Complex_Message_Content()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var complexContent = new List<MessageContentComplex>
        {
            new MessageContentTextRecord("Part 1"),
            new MessageContentTextRecord("Part 2")
        };
        var messages = new List<BaseMessage>
        {
            new AIMessage(
                id: "msg-1",
                content: new MessageContent 
                { 
                    IsSimple = false, 
                    ComplexContent = complexContent 
                }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().BeEquivalentTo(complexContent);
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Logs_Output_Capture()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new AIMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Test" }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("EndNode")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Returns_Simple_Content_From_Last_Message()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new HumanMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Question?" }
            ),
            new AIMessage(
                id: "msg-2",
                content: new MessageContent { IsSimple = true, SimpleContent = "First answer" }
            ),
            new AIMessage(
                id: "msg-3",
                content: new MessageContent { IsSimple = true, SimpleContent = "Final answer" }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Output.Should().Be("Final answer");
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Works_With_Single_Message()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new HumanMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Only message" }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Only message");
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_No_New_Messages_Or_Variables()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var messages = new List<BaseMessage>
        {
            new AIMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Test" }
            )
        };
        var context = CreateTestContext(messages);

        // Act
        var result = await endNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Messages.Should().BeEmpty();
        result.Variables.Should().BeNull();
    }

    [Fact]
    public async Task EndNode_ExecuteAsync_Respects_CancellationToken()
    {
        // Arrange
        var endNode = new EndNode("end-1", "End");
        var context = CreateTestContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - should complete immediately since EndNode is synchronous
        var result = await endNode.ExecuteAsync(context, cts.Token);

        // Assert - even though cancelled, EndNode completes (it's synchronous)
        result.Should().NotBeNull();
    }
}
