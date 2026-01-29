using System.Collections.ObjectModel;

namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// AI response message.
/// </summary>
public sealed class AIMessage : BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIMessage"/> class.
    /// </summary>
    public AIMessage(
        string id,
        MessageContent content,
        IReadOnlyList<ToolCall>? toolCalls = null,
        IReadOnlyList<InvalidToolCall>? invalidToolCalls = null,
        TokenUsage? usageMetadata = null,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
        : base(id, MessageType.AI, content, timestamp, name, additionalKwargs, responseMetadata)
    {
        ToolCalls = new ReadOnlyCollection<ToolCall>((toolCalls ?? []).ToList());
        InvalidToolCalls = new ReadOnlyCollection<InvalidToolCall>((invalidToolCalls ?? []).ToList());
        UsageMetadata = usageMetadata;
    }

    /// <summary>
    /// Gets the tool calls requested by the model.
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; }

    /// <summary>
    /// Gets invalid tool calls returned by the model.
    /// </summary>
    public IReadOnlyList<InvalidToolCall> InvalidToolCalls { get; }

    /// <summary>
    /// Gets token usage metadata.
    /// </summary>
    public TokenUsage? UsageMetadata { get; }
}
