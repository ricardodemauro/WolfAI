using System.Collections.ObjectModel;
using FluentAssertions;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Tests.Domain.Messages;

public class BaseMessageTests
{
    [Fact]
    public void HumanMessage_Sets_Type_And_Content()
    {
        var content = new MessageContent("hello");
        var message = new HumanMessage("m1", content);

        message.Id.Should().Be("m1");
        message.Type.Should().Be(MessageType.Human);
        message.Content.Should().Be(content);
        message.AdditionalKwargs.Should().BeOfType<ReadOnlyDictionary<string, object?>>();
        message.ResponseMetadata.Should().BeOfType<ReadOnlyDictionary<string, object?>>();
    }

    [Fact]
    public void AIMessage_Includes_ToolCalls_And_Metadata()
    {
        var content = new MessageContent("answer");
        var toolCall = new ToolCall("t1", "search", "{\"q\":\"test\"}");
        var invalidToolCall = new InvalidToolCall("t2", "search", "{}", "invalid args");
        var usage = new TokenUsage(10, 5);

        var message = new AIMessage(
            "m2",
            content,
            toolCalls: new[] { toolCall },
            invalidToolCalls: new[] { invalidToolCall },
            usageMetadata: usage);

        message.Type.Should().Be(MessageType.AI);
        message.ToolCalls.Should().ContainSingle().Which.Should().Be(toolCall);
        message.InvalidToolCalls.Should().ContainSingle().Which.Should().Be(invalidToolCall);
        message.UsageMetadata.Should().Be(usage);
    }

    [Fact]
    public void SystemMessage_Sets_Type()
    {
        var content = new MessageContent("system");
        var message = new SystemMessage("m3", content);

        message.Type.Should().Be(MessageType.System);
    }
}
