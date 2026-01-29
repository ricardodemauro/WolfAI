namespace WolfAI.Core.Domain.Results;

/// <summary>
/// Represents the outcome of executing a node.
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
    /// Variables set during node execution.
    /// </summary>
    public required Dictionary<string, object?> Variables { get; init; }

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
        Dictionary<string, object?>? variables = null,
        TimeSpan? duration = null)
    {
        return new NodeResult
        {
            Success = true,
            Output = output,
            Variables = variables ?? new(),
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
        Dictionary<string, object?>? variables = null,
        TimeSpan? duration = null)
    {
        return new NodeResult
        {
            Success = false,
            Output = output,
            Variables = variables ?? new(),
            Error = error,
            Duration = duration ?? TimeSpan.Zero
        };
    }
}
