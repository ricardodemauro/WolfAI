using System.Collections.ObjectModel;

namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Represents message content in either simple or complex form.
/// </summary>
public sealed class MessageContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContent"/> class for simple text content.
    /// </summary>
    public MessageContent(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Items = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContent"/> class for complex content.
    /// </summary>
    public MessageContent(IReadOnlyList<MessageContentComplex> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        Text = null;
        Items = new ReadOnlyCollection<MessageContentComplex>(items.ToList());
    }

    /// <summary>
    /// Gets the simple text content, if present.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the complex content items, if present.
    /// </summary>
    public IReadOnlyList<MessageContentComplex>? Items { get; }

    /// <summary>
    /// Gets whether this content is simple text.
    /// </summary>
    public bool IsSimple => Text != null;

    /// <summary>
    /// Gets whether this content is complex.
    /// </summary>
    public bool IsComplex => Items != null;
}
