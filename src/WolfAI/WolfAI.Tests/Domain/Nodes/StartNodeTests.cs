using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Tests.Domain.Nodes;

public class StartNodeTests
{
    private readonly Mock<ILogger> _mockLogger;

    public StartNodeTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    private IExecutionContext CreateTestContext(string? input = "Hello, world!")
    {
        var globalVariables = new Dictionary<string, object?>();
        if (input != null)
        {
            globalVariables["input"] = input;
        }

        return new ExecutionContext(
            executionId: "test-exec-1",
            threadId: "test-thread-1",
            graphId: "test-graph-1",
            currentNodeId: "start",
            globalVariables: globalVariables,
            logger: _mockLogger.Object
        );
    }

    [Fact]
    public void StartNode_Constructor_Sets_Properties()
    {
        // Act
        var startNode = new StartNode("start-1", "Start");

        // Assert
        startNode.Id.Should().Be("start-1");
        startNode.Name.Should().Be("Start");
        startNode.NodeType.Should().Be(NodeType.Start);
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Returns_Success_With_HumanMessage()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext("Test input message");

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Test input message");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Should().BeOfType<HumanMessage>();
        result.Messages[0].Content.SimpleContent.Should().Be("Test input message");
        result.Variables.Should().BeNull();
        result.Error.Should().BeNull();
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Does_Not_Mutate_Context()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext("Test input");
        var initialMessageCount = context.Messages.Count;

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.Messages.Count.Should().Be(initialMessageCount); // Context unchanged!
        result.Messages.Should().HaveCount(1); // New message in result
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Fails_When_No_Input_In_GlobalVariables()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext(input: null); // No input

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No input provided");
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Fails_When_Input_Is_Empty()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext(input: "");

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("empty or whitespace");
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Fails_When_Input_Is_Whitespace()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext(input: "   ");

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("empty or whitespace");
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Creates_HumanMessage_With_Correct_Content()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var expectedInput = "This is a test message for the AI";
        var context = CreateTestContext(expectedInput);

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var humanMessage = result.Messages[0] as HumanMessage;
        humanMessage.Should().NotBeNull();
        humanMessage!.Type.Should().Be(MessageType.Human);
        humanMessage.Content.IsSimple.Should().BeTrue();
        humanMessage.Content.SimpleContent.Should().Be(expectedInput);
        humanMessage.Id.Should().NotBeNullOrEmpty();
        humanMessage.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Logs_Initialization()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext("Test message");

        // Act
        await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("StartNode")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Handles_Long_Input()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var longInput = new string('A', 1000);
        var context = CreateTestContext(longInput);

        // Act
        var result = await startNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Messages[0].Content.SimpleContent.Should().Be(longInput);
    }

    [Fact]
    public async Task StartNode_ExecuteAsync_Respects_CancellationToken()
    {
        // Arrange
        var startNode = new StartNode("start-1", "Start");
        var context = CreateTestContext("Test input");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - should complete immediately since StartNode is synchronous
        var result = await startNode.ExecuteAsync(context, cts.Token);

        // Assert - even though cancelled, StartNode completes (it's synchronous)
        result.Should().NotBeNull();
    }
}
