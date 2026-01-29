namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Detailed token usage for specific token categories.
/// </summary>
public sealed class TokenDetailedUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenDetailedUsage"/> class.
    /// </summary>
    public TokenDetailedUsage(int audioTokens = 0, int cacheTokens = 0, int reasoningTokens = 0)
    {
        AudioTokens = audioTokens;
        CacheTokens = cacheTokens;
        ReasoningTokens = reasoningTokens;
    }

    /// <summary>
    /// Gets the audio token count.
    /// </summary>
    public int AudioTokens { get; }

    /// <summary>
    /// Gets the cache token count.
    /// </summary>
    public int CacheTokens { get; }

    /// <summary>
    /// Gets the reasoning token count.
    /// </summary>
    public int ReasoningTokens { get; }
}
