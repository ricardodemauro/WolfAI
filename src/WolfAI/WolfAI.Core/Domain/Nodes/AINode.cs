using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Core.Domain.Nodes;

/// <summary>
/// A flexible node type that accepts user-defined execution logic.
/// Supports any C# execution logic: LLM calls, database queries, custom algorithms, etc.
/// </summary>
public class AINode : Node
{
    /// <summary>
    /// User-provided execution logic.
    /// Receives read-only ExecutionContext and must return NodeResult with state changes.
    /// Logic must NOT mutate the ExecutionContext directly.
    /// </summary>
    public required Func<IExecutionContext, CancellationToken, Task<NodeResult>> ExecutionLogic { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AINode"/> class.
    /// </summary>
    /// <param name="id">Unique node identifier</param>
    /// <param name="name">Human-readable node name</param>
    public AINode(string id, string name)
        : base(id, name)
    {
    }

    /// <summary>
    /// Gets the node type (always AI).
    /// </summary>
    public override NodeType NodeType => NodeType.AI;

    /// <summary>
    /// Executes the AI node by delegating to user-provided execution logic.
    /// </summary>
    /// <param name="context">The execution context (read-only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NodeResult with state changes from user logic</returns>
    public override async Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Delegate to user-provided execution logic
            var result = await ExecutionLogic(context, cancellationToken);

            // Ensure duration is captured if not set by user
            if (result.Duration == TimeSpan.Zero)
            {
                result = new NodeResult
                {
                    Success = result.Success,
                    Output = result.Output,
                    Messages = result.Messages,
                    Variables = result.Variables,
                    Error = result.Error,
                    Duration = DateTime.UtcNow - startTime
                };
            }

            return result;
        }
        catch (OperationCanceledException ex)
        {
            // Handle cancellation gracefully
            return NodeResult.FailureResult(
                error: $"AINode {Id} execution was cancelled: {ex.Message}",
                duration: DateTime.UtcNow - startTime
            );
        }
        catch (Exception ex)
        {
            // Wrap any exceptions in NodeResult
            return NodeResult.FailureResult(
                error: $"AINode {Id} execution failed: {ex.GetType().Name}: {ex.Message}",
                duration: DateTime.UtcNow - startTime
            );
        }
    }
}
