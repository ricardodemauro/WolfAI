using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Core.Domain.Execution;

/// <summary>
/// Represents the execution context for node execution and routing decisions.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the unique identifier for this execution.
    /// </summary>
    string ExecutionId { get; }

    /// <summary>
    /// Gets the thread/conversation identifier for this execution.
    /// </summary>
    string ThreadId { get; }

    /// <summary>
    /// Gets the graph identifier being executed.
    /// </summary>
    string GraphId { get; }

    /// <summary>
    /// Gets or sets the current node being executed.
    /// </summary>
    string CurrentNodeId { get; set; }

    /// <summary>
    /// Gets the global variables shared across all nodes (read-only).
    /// </summary>
    IReadOnlyDictionary<string, object?> GlobalVariables { get; }

    /// <summary>
    /// Gets the variable scope for managing hierarchical variables (read-only).
    /// </summary>
    VariableScope Variables { get; }

    /// <summary>
    /// Gets the message history for this execution (read-only).
    /// </summary>
    IReadOnlyList<BaseMessage> Messages { get; }

    /// <summary>
    /// Gets the execution history as a stack of node IDs (read-only).
    /// </summary>
    IReadOnlyList<string> NodeExecutionHistory { get; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Gets the logger for this execution.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Gets the activity source for distributed tracing.
    /// </summary>
    ActivitySource? ActivitySource { get; }

    /// <summary>
    /// Gets or sets the current activity for distributed tracing.
    /// </summary>
    Activity? CurrentActivity { get; set; }

    /// <summary>
    /// Gets the cancellation token for graceful shutdown.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the execution metrics.
    /// </summary>
    ExecutionMetrics Metrics { get; }

    /// <summary>
    /// Gets the UTC timestamp when this execution started.
    /// </summary>
    DateTime StartedAt { get; }

    /// <summary>
    /// Gets the metadata dictionary for storing execution-specific data (read-only).
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Gets the elapsed time since execution started.
    /// </summary>
    TimeSpan Elapsed { get; }
}
