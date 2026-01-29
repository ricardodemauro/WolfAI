namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Function call message.
/// </summary>
public sealed class FunctionMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionMessage"/> class.
    /// </summary>
    public FunctionMessage(
        string id,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.Function, content, timestamp, name, additionalKwargs, responseMetadata)
    {
    }
}
