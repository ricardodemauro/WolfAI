using FluentAssertions;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Tests.Domain.Results;

public class NodeResultTests
{
    [Fact]
    public void SuccessResult_Creates_Result_With_Success_True()
    {
        // Arrange & Act
        var result = NodeResult.SuccessResult(
            output: "test output",
            variables: new() { { "key", "value" } },
            duration: TimeSpan.FromSeconds(1));

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().Be("test output");
        result.Variables.Should().ContainKey("key").WhoseValue.Should().Be("value");
        result.Duration.Should().Be(TimeSpan.FromSeconds(1));
        result.Error.Should().BeNull();
    }

    [Fact]
    public void SuccessResult_With_Defaults_Creates_Valid_Result()
    {
        // Arrange & Act
        var result = NodeResult.SuccessResult();

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().BeNull();
        result.Variables.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void FailureResult_Creates_Result_With_Success_False()
    {
        // Arrange & Act
        var result = NodeResult.FailureResult(
            error: "Test error",
            output: "partial output",
            variables: new() { { "key", "value" } },
            duration: TimeSpan.FromSeconds(2));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Test error");
        result.Output.Should().Be("partial output");
        result.Variables.Should().ContainKey("key").WhoseValue.Should().Be("value");
        result.Duration.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void FailureResult_With_Minimal_Parameters()
    {
        // Arrange & Act
        var result = NodeResult.FailureResult("Something went wrong");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Something went wrong");
        result.Output.Should().BeNull();
        result.Variables.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
    }
}
