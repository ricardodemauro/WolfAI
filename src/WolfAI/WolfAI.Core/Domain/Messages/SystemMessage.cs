namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// System message.
/// </summary>
public sealed class SystemMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemMessage"/> class.
    /// </summary>
    public SystemMessage(
        string id,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.System, content, timestamp, name, additionalKwargs, responseMetadata)
    {
    }
}
