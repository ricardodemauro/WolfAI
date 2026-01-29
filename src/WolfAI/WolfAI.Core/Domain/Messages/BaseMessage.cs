using System.Collections.ObjectModel;

namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Base message abstraction.
/// </summary>
public abstract class BaseMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessage"/> class.
    /// </summary>
    protected BaseMessage(
        string id,
        MessageType type,
        MessageContent content,
        DateTimeOffset? timestamp = null,
        string? name = null,
        IReadOnlyDictionary<string, object?>? additionalKwargs = null,
        IReadOnlyDictionary<string, object?>? responseMetadata = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Type = type;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        Name = name;
        AdditionalKwargs = ToReadOnlyDictionary(additionalKwargs);
        ResponseMetadata = ToReadOnlyDictionary(responseMetadata);
    }

    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the message timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the message type.
    /// </summary>
    public MessageType Type { get; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public MessageContent Content { get; }

    /// <summary>
    /// Gets the optional message name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets additional keyword arguments.
    /// </summary>
    public IReadOnlyDictionary<string, object?> AdditionalKwargs { get; }

    /// <summary>
    /// Gets response metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ResponseMetadata { get; }

    private static IReadOnlyDictionary<string, object?> ToReadOnlyDictionary(
        IReadOnlyDictionary<string, object?>? source)
    {
        if (source == null)
        {
            return new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>());
        }

        return new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(source));
    }
}
