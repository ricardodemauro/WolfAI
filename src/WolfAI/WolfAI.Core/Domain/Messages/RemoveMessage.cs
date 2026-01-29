namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Message representing removal of content.
/// </summary>
public sealed class RemoveMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveMessage"/> class.
    /// </summary>
    public RemoveMessage(
        string id,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.Remove, content, timestamp, name, additionalKwargs, responseMetadata)
    {
    }
}
