namespace WolfAI.Core.Domain.Messages;

/// <summary>
/// Represents a tool call requested by the model.
/// </summary>
public sealed class ToolCall
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCall"/> class.
    /// </summary>
    public ToolCall(string id, string name, string arguments)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    /// <summary>
    /// Gets the tool call identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tool arguments (serialized).
    /// </summary>
    public string Arguments { get; }
}
