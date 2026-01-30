using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Core.Domain.Nodes;

/// <summary>
/// Special exit point node for graph execution.
/// Captures final output from the execution context and terminates the workflow.
/// </summary>
public sealed class EndNode : Node
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndNode"/> class.
    /// </summary>
    /// <param name="id">Unique node identifier</param>
    /// <param name="name">Human-readable node name</param>
    public EndNode(string id, string name)
        : base(id, name)
    {
    }

    /// <summary>
    /// Gets the node type (always End).
    /// </summary>
    public override NodeType NodeType => NodeType.End;

    /// <summary>
    /// Executes the end node by capturing final output from the message history.
    /// </summary>
    /// <param name="context">The execution context (read-only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NodeResult with final output (no new messages or variables)</returns>
    public override Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Capture final output from last message
            var lastMessage = context.Messages.LastOrDefault();
            
            object? finalOutput;
            if (lastMessage != null)
            {
                // Extract content from last message
                finalOutput = lastMessage.Content.IsSimple
                    ? lastMessage.Content.Text
                    : lastMessage.Content.Items;
            }
            else
            {
                // No messages in history - use a default value
                finalOutput = "No output generated";
            }

            context.Logger?.LogInformation(
                "EndNode {NodeId} captured final output from {MessageCount} messages",
                Id,
                context.Messages.Count
            );

            // EndNode doesn't produce new messages or variables
            // It just captures the final state
            return Task.FromResult(NodeResult.SuccessResult(
                output: finalOutput,
                messages: Array.Empty<BaseMessage>(), // No new messages
                variables: null, // No new variables
                duration: DateTime.UtcNow - startTime
            ));
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(
                ex,
                "EndNode {NodeId} failed with error: {ErrorMessage}",
                Id,
                ex.Message
            );

            return Task.FromResult(NodeResult.FailureResult(
                error: $"EndNode execution failed: {ex.Message}",
                duration: DateTime.UtcNow - startTime
            ));
        }
    }
}
