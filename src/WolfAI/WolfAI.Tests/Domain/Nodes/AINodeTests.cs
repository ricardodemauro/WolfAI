using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;
using ExecutionContextClass = WolfAI.Core.Domain.Execution.ExecutionContext;

namespace WolfAI.Tests.Domain.Nodes;

public class AINodeTests
{
    private readonly Mock<ILogger> _mockLogger;

    public AINodeTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    private IExecutionContext CreateTestContext(
        string? input = null,
        IEnumerable<BaseMessage>? messages = null)
    {
        var globalVariables = new Dictionary<string, object?>();
        if (input != null)
        {
            globalVariables["input"] = input;
        }

        return new ExecutionContextClass(
            executionId: "test-exec-1",
            threadId: "test-thread-1",
            graphId: "test-graph-1",
            currentNodeId: "ai-node",
            globalVariables: globalVariables,
            messages: messages,
            logger: _mockLogger.Object
        );
    }

    [Fact]
    public void AINode_Constructor_Sets_Properties()
    {
        // Act
        var aiNode = new AINode("ai-1", "AI Classifier");

        // Assert
        aiNode.Id.Should().Be("ai-1");
        aiNode.Name.Should().Be("AI Classifier");
        aiNode.NodeType.Should().Be(NodeType.AI);
    }

    [Fact]
    public void AINode_Constructor_Requires_ExecutionLogic()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var aiNode = new AINode("ai-1", "AI Node")
            {
                ExecutionLogic = null!
            };
        });

        exception.ParamName.Should().Be("ExecutionLogic");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Delegates_To_ExecutionLogic()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                return NodeResult.SuccessResult(
                    output: "Test output",
                    messages: new List<BaseMessage>
                    {
                        new AIMessage(
                            id: "msg-1",
                            content: new MessageContent { IsSimple = true, SimpleContent = "AI Response" }
                        )
                    }
                );
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Test output");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Type.Should().Be(MessageType.AI);
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Does_Not_Mutate_Context()
    {
        // Arrange
        var context = CreateTestContext();
        var initialMessageCount = context.Messages.Count;

        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (ctx, ct) =>
            {
                return NodeResult.SuccessResult(
                    output: "output",
                    messages: new List<BaseMessage>
                    {
                        new AIMessage(
                            id: "msg-1",
                            content: new MessageContent { IsSimple = true, SimpleContent = "Response" }
                        )
                    }
                );
            }
        };

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.Messages.Count.Should().Be(initialMessageCount);  // Context unchanged!
        result.Messages.Should().HaveCount(1);  // New message in result
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Supports_Async_Operations()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                // Simulate async LLM API call
                await Task.Delay(100, ct);

                return NodeResult.SuccessResult(
                    output: "Async result",
                    messages: new List<BaseMessage>
                    {
                        new AIMessage(
                            id: "msg-1",
                            content: new MessageContent { IsSimple = true, SimpleContent = "Async response" }
                        )
                    }
                );
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Async result");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Returns_Variables_From_Logic()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                return NodeResult.SuccessResult(
                    output: "result",
                    messages: new List<BaseMessage>(),
                    variables: new Dictionary<string, object?>
                    {
                        ["processedData"] = "some data",
                        ["tokenCount"] = 250
                    }
                );
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Variables.Should().NotBeNull();
        result.Variables.Should().ContainKey("processedData");
        result.Variables.Should().ContainKey("tokenCount");
        result.Variables["processedData"].Should().Be("some data");
        result.Variables["tokenCount"].Should().Be(250);
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Catches_Exceptions_And_Returns_Failure()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                throw new InvalidOperationException("Something went wrong");
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Something went wrong");
        result.Error.Should().Contain("InvalidOperationException");
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Handles_Cancellation_Token()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                try
                {
                    await Task.Delay(1000, ct);
                    return NodeResult.SuccessResult(output: "completed");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        };

        var context = CreateTestContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await aiNode.ExecuteAsync(context, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Respects_CancellationToken_From_Logic()
    {
        // Arrange
        var cancellationWasCalled = false;

        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return NodeResult.SuccessResult(output: "completed");
            }
        };

        var context = CreateTestContext();
        var cts = new CancellationTokenSource();
        
        // Cancel after a very short delay
        cts.CancelAfter(10);

        // Act
        var result = await aiNode.ExecuteAsync(context, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Tracks_Execution_Duration()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                await Task.Delay(100, ct);
                return NodeResult.SuccessResult(output: "output", duration: TimeSpan.FromMilliseconds(100));
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Has_Default_Duration_If_Not_Set()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                return new NodeResult
                {
                    Success = true,
                    Output = "output",
                    Messages = Array.Empty<BaseMessage>(),
                    Variables = new Dictionary<string, object?>(),
                    Duration = TimeSpan.Zero  // Not set by logic
                };
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Can_Read_From_ExecutionContext()
    {
        // Arrange
        var testMessages = new List<BaseMessage>
        {
            new HumanMessage(
                id: "msg-1",
                content: new MessageContent { IsSimple = true, SimpleContent = "Hello" }
            ),
            new AIMessage(
                id: "msg-2",
                content: new MessageContent { IsSimple = true, SimpleContent = "Hi there" }
            )
        };

        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                // Read from context
                var messageCount = context.Messages.Count;
                var lastMessage = context.Messages.LastOrDefault();

                return NodeResult.SuccessResult(
                    output: messageCount,
                    variables: new Dictionary<string, object?>
                    {
                        ["messageCount"] = messageCount,
                        ["lastMessageType"] = lastMessage?.Type.ToString()
                    }
                );
            }
        };

        var context = CreateTestContext(messages: testMessages);

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Output.Should().Be(2);
        result.Variables["messageCount"].Should().Be(2);
        result.Variables["lastMessageType"].Should().Be("AI");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Can_Access_GlobalVariables()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                var userId = context.GlobalVariables.TryGetValue("userId", out var value)
                    ? value?.ToString()
                    : "unknown";

                return NodeResult.SuccessResult(
                    output: $"User: {userId}",
                    variables: new Dictionary<string, object?> { ["userIdProcessed"] = userId }
                );
            }
        };

        var globalVariables = new Dictionary<string, object?> { ["userId"] = "user-123" };
        var context = new ExecutionContext(
            executionId: "test",
            threadId: "test",
            graphId: "test",
            currentNodeId: "ai",
            globalVariables: globalVariables
        );

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Output.Should().Be("User: user-123");
        result.Variables["userIdProcessed"].Should().Be("user-123");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Supports_Complex_LLM_Like_Logic()
    {
        // Arrange - Simulate a realistic LLM node
        var aiNode = new AINode("llm-classifier", "LLM Classifier")
        {
            ExecutionLogic = async (context, ct) =>
            {
                // 1. Read input
                var userInput = context.GlobalVariables.TryGetValue("input", out var val)
                    ? val?.ToString()
                    : "";

                if (string.IsNullOrEmpty(userInput))
                {
                    return NodeResult.FailureResult("No input provided");
                }

                // 2. Simulate LLM API call
                await Task.Delay(50, ct);  // Simulate network latency
                var classification = userInput.Length > 10 ? "long" : "short";
                var tokensUsed = 150;

                // 3. Return state changes
                var messages = new List<BaseMessage>
                {
                    new AIMessage(
                        id: Guid.NewGuid().ToString(),
                        content: new MessageContent
                        {
                            IsSimple = true,
                            SimpleContent = $"Classification: {classification}"
                        }
                    )
                };

                var variables = new Dictionary<string, object?>
                {
                    ["classification"] = classification,
                    ["tokensUsed"] = tokensUsed,
                    ["textLength"] = userInput.Length
                };

                return NodeResult.SuccessResult(
                    output: classification,
                    messages: messages,
                    variables: variables
                );
            }
        };

        var globalVariables = new Dictionary<string, object?> { ["input"] = "This is a long input text for testing" };
        var context = new ExecutionContext(
            executionId: "test",
            threadId: "test",
            graphId: "test",
            currentNodeId: "llm-classifier",
            globalVariables: globalVariables
        );

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("long");
        result.Messages.Should().HaveCount(1);
        result.Variables["tokensUsed"].Should().Be(150);
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Handles_Null_Output()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                return NodeResult.SuccessResult(
                    output: null,
                    messages: new List<BaseMessage>()
                );
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().BeNull();
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Handles_Multiple_Exception_Types()
    {
        // Test different exception types

        // ArgumentException
        var aiNode1 = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                throw new ArgumentException("Invalid argument");
            }
        };

        var context = CreateTestContext();
        var result1 = await aiNode1.ExecuteAsync(context, CancellationToken.None);
        result1.Success.Should().BeFalse();
        result1.Error.Should().Contain("ArgumentException");

        // InvalidOperationException
        var aiNode2 = new AINode("ai-2", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                throw new InvalidOperationException("Invalid operation");
            }
        };

        var result2 = await aiNode2.ExecuteAsync(context, CancellationToken.None);
        result2.Success.Should().BeFalse();
        result2.Error.Should().Contain("InvalidOperationException");

        // TimeoutException
        var aiNode3 = new AINode("ai-3", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                throw new TimeoutException("Operation timed out");
            }
        };

        var result3 = await aiNode3.ExecuteAsync(context, CancellationToken.None);
        result3.Success.Should().BeFalse();
        result3.Error.Should().Contain("TimeoutException");
    }

    [Fact]
    public async Task AINode_ExecuteAsync_Returns_Failure_Result_With_Error_Details()
    {
        // Arrange
        var aiNode = new AINode("ai-1", "AI Node")
        {
            ExecutionLogic = async (context, ct) =>
            {
                throw new Exception("Critical error occurred");
            }
        };

        var context = CreateTestContext();

        // Act
        var result = await aiNode.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.Error.Should().Contain("Critical error occurred");
        result.Messages.Should().BeEmpty();
        result.Variables?.Count.Should().Be(0);
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }
}
