namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Represents a tool call that failed validation.
/// </summary>
public sealed class InvalidToolCall
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidToolCall"/> class.
    /// </summary>
    public InvalidToolCall(string id, string? name, string? arguments, string error)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name;
        Arguments = arguments;
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Gets the tool call identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the tool arguments (serialized).
    /// </summary>
    public string? Arguments { get; }

    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string Error { get; }
}
