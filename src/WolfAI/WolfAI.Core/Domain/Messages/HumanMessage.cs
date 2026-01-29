namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Human input message.
/// </summary>
public sealed class HumanMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HumanMessage"/> class.
    /// </summary>
    public HumanMessage(
        string id,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.Human, content, timestamp, name, additionalKwargs, responseMetadata)
    {
    }
}
