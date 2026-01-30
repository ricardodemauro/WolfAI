using WolfAI.Core.Domain.Messages;

namespace WolfAI.Core.Domain.Results;

/// <summary>
/// Represents the outcome of executing a node.
/// Contains state changes to be applied by the execution engine (immutable pattern).
/// </summary>
public class NodeResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeResult"/> class.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The output produced by the node execution.
    /// </summary>
    public required object? Output { get; init; }

    /// <summary>
    /// Messages to be appended to the execution context message history.
    /// The graph execution engine is responsible for creating a new context with these messages.
    /// </summary>
    public required IReadOnlyList<BaseMessage> Messages { get; init; }

    /// <summary>
    /// Variables to be added/updated in the execution context.
    /// The graph execution engine is responsible for creating a new context with these variables.
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Variables { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Time taken to execute the node.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful node result.
    /// </summary>
    public static NodeResult SuccessResult(
        object? output = null,
        IReadOnlyList<BaseMessage>? messages = null,
        IReadOnlyDictionary<string, object?>? variables = null,
        TimeSpan? duration = null)
    {
        return new NodeResult
        {
            Success = true,
            Output = output,
            Messages = messages ?? Array.Empty<BaseMessage>(),
            Variables = variables ?? new Dictionary<string, object?>(),
            Error = null,
            Duration = duration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates a failed node result.
    /// </summary>
    public static NodeResult FailureResult(
        string error,
        object? output = null,
        IReadOnlyList<BaseMessage>? messages = null,
        IReadOnlyDictionary<string, object?>? variables = null,
        TimeSpan? duration = null)
    {
        return new NodeResult
        {
            Success = false,
            Output = output,
            Messages = messages ?? Array.Empty<BaseMessage>(),
            Variables = variables ?? new Dictionary<string, object?>(),
            Error = error,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
