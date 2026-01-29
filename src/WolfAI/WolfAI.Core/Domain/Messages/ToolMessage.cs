namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Tool output message.
/// </summary>
public sealed class ToolMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolMessage"/> class.
    /// </summary>
    public ToolMessage(
        string id,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.Tool, content, timestamp, name, additionalKwargs, responseMetadata)
    {
    }
}
