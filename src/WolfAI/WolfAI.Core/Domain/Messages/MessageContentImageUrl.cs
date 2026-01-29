namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Image URL content item.
/// </summary>
public sealed class MessageContentImageUrl : MessageContentComplex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContentImageUrl"/> class.
    /// </summary>
    public MessageContentImageUrl(string url, ImageDetail detail = ImageDetail.Auto)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Detail = detail;
    }

    /// <summary>
    /// Gets the image URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the image detail level.
    /// </summary>
    public ImageDetail Detail { get; }
}
