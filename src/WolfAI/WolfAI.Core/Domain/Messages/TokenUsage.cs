namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Aggregated token usage information.
/// </summary>
public sealed class TokenUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenUsage"/> class.
    /// </summary>
    public TokenUsage(
        int inputTokens,
        int outputTokens,
        TokenDetailedUsage? inputTokenDetails = null,
        TokenDetailedUsage? outputTokenDetails = null,
        int? totalTokens = null)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        InputTokenDetails = inputTokenDetails;
        OutputTokenDetails = outputTokenDetails;
        TotalTokens = totalTokens ?? inputTokens + outputTokens;
    }

    /// <summary>
    /// Gets the input token count.
    /// </summary>
    public int InputTokens { get; }

    /// <summary>
    /// Gets the output token count.
    /// </summary>
    public int OutputTokens { get; }

    /// <summary>
    /// Gets the total token count.
    /// </summary>
    public int TotalTokens { get; }

    /// <summary>
    /// Gets detailed input token usage.
    /// </summary>
    public TokenDetailedUsage? InputTokenDetails { get; }

    /// <summary>
    /// Gets detailed output token usage.
    /// </summary>
    public TokenDetailedUsage? OutputTokenDetails { get; }
}
