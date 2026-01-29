using FluentAssertions;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Tests.Domain.Messages;

public class TokenUsageTests
{
    [Fact]
    public void TokenUsage_Computes_Total_When_Not_Provided()
    {
        var usage = new TokenUsage(7, 3);

        usage.TotalTokens.Should().Be(10);
    }

    [Fact]
    public void TokenUsage_Uses_Total_When_Provided()
    {
        var usage = new TokenUsage(7, 3, totalTokens: 20);

        usage.TotalTokens.Should().Be(20);
    }

    [Fact]
    public void TokenUsage_Holds_Detailed_Usage()
    {
        var inputDetails = new TokenDetailedUsage(audioTokens: 1, cacheTokens: 2, reasoningTokens: 3);
        var outputDetails = new TokenDetailedUsage(audioTokens: 0, cacheTokens: 1, reasoningTokens: 4);

        var usage = new TokenUsage(5, 6, inputDetails, outputDetails);

        usage.InputTokenDetails.Should().Be(inputDetails);
        usage.OutputTokenDetails.Should().Be(outputDetails);
    }
}
