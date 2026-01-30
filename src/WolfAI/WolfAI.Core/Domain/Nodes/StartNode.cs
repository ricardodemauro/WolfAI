using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Messages;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Core.Domain.Nodes;

/// <summary>
/// Special entry point node for graph execution.
/// Extracts initial input and creates the first HumanMessage in the conversation.
/// </summary>
public sealed class StartNode : Node
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartNode"/> class.
    /// </summary>
    /// <param name="id">Unique node identifier</param>
    /// <param name="name">Human-readable node name</param>
    public StartNode(string id, string name)
        : base(id, name)
    {
    }

    /// <summary>
    /// Gets the node type (always Start).
    /// </summary>
    public override NodeType NodeType => NodeType.Start;

    /// <summary>
    /// Executes the start node by extracting initial input and creating a HumanMessage.
    /// </summary>
    /// <param name="context">The execution context (read-only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NodeResult with initial HumanMessage to be added to context</returns>
    public override Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Extract initial input from global variables
            if (!context.GlobalVariables.TryGetValue("input", out var inputValue))
            {
                return Task.FromResult(NodeResult.FailureResult(
                    error: "No input provided in GlobalVariables['input']",
                    duration: DateTime.UtcNow - startTime
                ));
            }

            var input = inputValue?.ToString();
            if (string.IsNullOrWhiteSpace(input))
            {
                return Task.FromResult(NodeResult.FailureResult(
                    error: "Input is empty or whitespace",
                    duration: DateTime.UtcNow - startTime
                ));
            }

            // Create initial HumanMessage (to be added by execution engine)
            var messages = new List<BaseMessage>
            {
                new HumanMessage(
                    id: Guid.NewGuid().ToString(),
                    content: new MessageContent(input)
                )
            };

            context.Logger?.LogDebug(
                "StartNode {NodeId} initialized with input: {InputPreview}",
                Id,
                input.Length > 50 ? input.Substring(0, 50) + "..." : input
            );

            return Task.FromResult(NodeResult.SuccessResult(
                output: input,
                messages: messages,
                variables: null, // No variables to add
                duration: DateTime.UtcNow - startTime
            ));
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(
                ex,
                "StartNode {NodeId} failed with error: {ErrorMessage}",
                Id,
                ex.Message
            );

            return Task.FromResult(NodeResult.FailureResult(
                error: $"StartNode execution failed: {ex.Message}",
                duration: DateTime.UtcNow - startTime
            ));
        }
    }
}
