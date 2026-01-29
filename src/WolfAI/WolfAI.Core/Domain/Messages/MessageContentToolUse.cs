namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Tool use content item.
/// </summary>
public sealed class MessageContentToolUse : MessageContentComplex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContentToolUse"/> class.
    /// </summary>
    public MessageContentToolUse(ToolCall toolCall)
    {
        ToolCall = toolCall ?? throw new ArgumentNullException(nameof(toolCall));
    }

    /// <summary>
    /// Gets the tool call represented by this content item.
    /// </summary>
    public ToolCall ToolCall { get; }
}
