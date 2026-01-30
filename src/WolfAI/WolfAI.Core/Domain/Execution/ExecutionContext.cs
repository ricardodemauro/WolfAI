using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Messages;

namespace WolfAI.Core.Domain.Execution;

/// <summary>
/// Represents the runtime execution context for graph execution.
/// Encapsulates all state, dependencies, and metadata needed during node execution.
/// This class is IMMUTABLE - the graph execution engine creates new instances with updated state.
/// </summary>
public class ExecutionContext : IExecutionContext
{
    private readonly List<BaseMessage> _messages;
    private readonly List<string> _nodeExecutionHistory;
    private readonly Dictionary<string, object?> _globalVariables;
    private readonly Dictionary<string, object?> _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
    /// </summary>
    public ExecutionContext(
        string executionId,
        string threadId,
        string graphId,
        string currentNodeId,
        IEnumerable<BaseMessage>? messages = null,
        IEnumerable<string>? nodeExecutionHistory = null,
        IDictionary<string, object?>? globalVariables = null,
        IDictionary<string, object?>? metadata = null,
        IServiceProvider? serviceProvider = null,
        ILogger? logger = null,
        ActivitySource? activitySource = null,
        CancellationToken cancellationToken = default,
        ExecutionMetrics? metrics = null,
        DateTime? startedAt = null)
    {
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        ThreadId = threadId ?? throw new ArgumentNullException(nameof(threadId));
        GraphId = graphId ?? throw new ArgumentNullException(nameof(graphId));
        CurrentNodeId = currentNodeId ?? throw new ArgumentNullException(nameof(currentNodeId));

        _messages = messages?.ToList() ?? new List<BaseMessage>();
        _nodeExecutionHistory = nodeExecutionHistory?.ToList() ?? new List<string>();
        _globalVariables = globalVariables != null 
            ? new Dictionary<string, object?>(globalVariables) 
            : new Dictionary<string, object?>();
        _metadata = metadata != null 
            ? new Dictionary<string, object?>(metadata) 
            : new Dictionary<string, object?>();

        Variables = new VariableScope(_globalVariables);
        ServiceProvider = serviceProvider;
        Logger = logger;
        ActivitySource = activitySource;
        CurrentActivity = null;
        CancellationToken = cancellationToken;
        Metrics = metrics ?? new ExecutionMetrics();
        StartedAt = startedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for this execution.
    /// </summary>
    public string ExecutionId { get; }

    /// <summary>
    /// Gets the thread/conversation identifier for this execution.
    /// </summary>
    public string ThreadId { get; }

    /// <summary>
    /// Gets the graph identifier being executed.
    /// </summary>
    public string GraphId { get; }

    /// <summary>
    /// Gets or sets the current node being executed.
    /// </summary>
    public string CurrentNodeId { get; set; }

    /// <summary>
    /// Gets the global variables shared across all nodes (read-only).
    /// </summary>
    public IReadOnlyDictionary<string, object?> GlobalVariables => _globalVariables;

    /// <summary>
    /// Gets the variable scope for managing hierarchical variables (read-only).
    /// </summary>
    public VariableScope Variables { get; }

    /// <summary>
    /// Gets the message history for this execution (read-only).
    /// </summary>
    public IReadOnlyList<BaseMessage> Messages => _messages;

    /// <summary>
    /// Gets the execution history as a readonly list in reverse chronological order.
    /// </summary>
    public IReadOnlyList<string> NodeExecutionHistory => _nodeExecutionHistory;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Gets the logger for this execution.
    /// </summary>
    public ILogger? Logger { get; }

    /// <summary>
    /// Gets the activity source for distributed tracing.
    /// </summary>
    public ActivitySource? ActivitySource { get; }

    /// <summary>
    /// Gets or sets the current activity for distributed tracing.
    /// </summary>
    public Activity? CurrentActivity { get; set; }

    /// <summary>
    /// Gets the cancellation token for graceful shutdown.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the execution metrics.
    /// </summary>
    public ExecutionMetrics Metrics { get; }

    /// <summary>
    /// Gets the UTC timestamp when this execution started.
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// Gets the metadata dictionary for storing execution-specific data (read-only).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;

    /// <summary>
    /// Gets the elapsed time since execution started.
    /// </summary>
    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

    /// <summary>
    /// Creates a new ExecutionContext with appended messages and variables.
    /// This is the ONLY way to update execution state - maintains immutability.
    /// Used by the graph execution engine after node execution.
    /// </summary>
    /// <param name="newMessages">Messages to append to history</param>
    /// <param name="newVariables">Variables to add/update</param>
    /// <param name="newNodeId">Optional: Update current node ID</param>
    /// <param name="recordNodeExecution">Optional: Node ID to record in execution history</param>
    /// <returns>New immutable ExecutionContext with merged state</returns>
    public ExecutionContext WithUpdates(
        IEnumerable<BaseMessage>? newMessages = null,
        IDictionary<string, object?>? newVariables = null,
        string? newNodeId = null,
        string? recordNodeExecution = null)
    {
        // Merge messages
        var mergedMessages = new List<BaseMessage>(_messages);
        if (newMessages != null)
        {
            mergedMessages.AddRange(newMessages);
        }

        // Merge global variables
        var mergedVariables = new Dictionary<string, object?>(_globalVariables);
        if (newVariables != null)
        {
            foreach (var kvp in newVariables)
            {
                mergedVariables[kvp.Key] = kvp.Value;
            }
        }

        // Merge execution history
        var mergedHistory = new List<string>(_nodeExecutionHistory);
        if (recordNodeExecution != null)
        {
            mergedHistory.Add(recordNodeExecution);
        }

        return new ExecutionContext(
            executionId: ExecutionId,
            threadId: ThreadId,
            graphId: GraphId,
            currentNodeId: newNodeId ?? CurrentNodeId,
            messages: mergedMessages,
            nodeExecutionHistory: mergedHistory,
            globalVariables: mergedVariables,
            metadata: _metadata, // Metadata doesn't change during execution
            serviceProvider: ServiceProvider,
            logger: Logger,
            activitySource: ActivitySource,
            cancellationToken: CancellationToken,
            metrics: Metrics, // Metrics object is mutable (for performance tracking)
            startedAt: StartedAt)
        {
            CurrentActivity = CurrentActivity
        };
    }

    /// <summary>
    /// Creates a snapshot of the current execution context state for checkpointing.
    /// </summary>
    /// <returns>A snapshot containing the current state</returns>
    public ExecutionContextSnapshot CreateSnapshot()
    {
        return new ExecutionContextSnapshot
        {
            ExecutionId = ExecutionId,
            ThreadId = ThreadId,
            GraphId = GraphId,
            CurrentNodeId = CurrentNodeId,
            GlobalVariables = new Dictionary<string, object?>(_globalVariables),
            Messages = new List<BaseMessage>(_messages),
            NodeExecutionHistory = new List<string>(_nodeExecutionHistory),
            Metrics = new ExecutionMetrics
            {
                NodesExecuted = Metrics.NodesExecuted,
                TotalTokensUsed = Metrics.TotalTokensUsed,
                TotalEstimatedCost = Metrics.TotalEstimatedCost,
                TotalDuration = Metrics.TotalDuration
            },
            StartedAt = StartedAt,
            Metadata = new Dictionary<string, object?>(_metadata)
        };
    }

    /// <summary>
    /// Creates a new ExecutionContext from a snapshot.
    /// Used for checkpoint restoration.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from</param>
    /// <param name="serviceProvider">Service provider for the restored context</param>
    /// <param name="logger">Logger for the restored context</param>
    /// <param name="activitySource">Activity source for the restored context</param>
    /// <param name="cancellationToken">Cancellation token for the restored context</param>
    /// <returns>New ExecutionContext with state from snapshot</returns>
    public static ExecutionContext FromSnapshot(
        ExecutionContextSnapshot snapshot,
        IServiceProvider? serviceProvider = null,
        ILogger? logger = null,
        ActivitySource? activitySource = null,
        CancellationToken cancellationToken = default)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        return new ExecutionContext(
            executionId: snapshot.ExecutionId,
            threadId: snapshot.ThreadId,
            graphId: snapshot.GraphId,
            currentNodeId: snapshot.CurrentNodeId,
            messages: snapshot.Messages,
            nodeExecutionHistory: snapshot.NodeExecutionHistory,
            globalVariables: snapshot.GlobalVariables,
            metadata: snapshot.Metadata,
            serviceProvider: serviceProvider,
            logger: logger,
            activitySource: activitySource,
            cancellationToken: cancellationToken,
            metrics: snapshot.Metrics,
            startedAt: snapshot.StartedAt);
    }
}

/// <summary>
/// Represents a snapshot of execution context state for checkpointing.
/// This is used for serialization and checkpoint restoration.
/// </summary>
public class ExecutionContextSnapshot
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public required string ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the thread ID.
    /// </summary>
    public required string ThreadId { get; set; }

    /// <summary>
    /// Gets or sets the graph ID.
    /// </summary>
    public required string GraphId { get; set; }

    /// <summary>
    /// Gets or sets the current node ID.
    /// </summary>
    public required string CurrentNodeId { get; set; }

    /// <summary>
    /// Gets or sets the global variables.
    /// </summary>
    public required Dictionary<string, object?> GlobalVariables { get; set; }

    /// <summary>
    /// Gets or sets the message history.
    /// </summary>
    public required List<BaseMessage> Messages { get; set; }

    /// <summary>
    /// Gets or sets the node execution history.
    /// </summary>
    public required List<string> NodeExecutionHistory { get; set; }

    /// <summary>
    /// Gets or sets the execution metrics.
    /// </summary>
    public required ExecutionMetrics Metrics { get; set; }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public required DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public required Dictionary<string, object?> Metadata { get; set; }
}
