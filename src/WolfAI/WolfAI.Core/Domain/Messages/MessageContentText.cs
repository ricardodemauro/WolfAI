namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Text content item.
/// </summary>
public sealed class MessageContentText : MessageContentComplex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContentText"/> class.
    /// </summary>
    public MessageContentText(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string Text { get; }
}
