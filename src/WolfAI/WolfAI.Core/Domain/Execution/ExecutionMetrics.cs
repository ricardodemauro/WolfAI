using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Core.Domain.Execution;

/// <summary>
/// Tracks execution statistics and metrics for a graph execution.
/// </summary>
public class ExecutionMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionMetrics"/> class.
    /// </summary>
    public ExecutionMetrics()
    {
        NodesExecuted = 0;
        TotalTokensUsed = 0;
        TotalEstimatedCost = 0m;
        TotalDuration = TimeSpan.Zero;
        NodeTypeCounters = new Dictionary<NodeType, int>
        {
            { NodeType.Start, 0 },
            { NodeType.End, 0 },
            { NodeType.AI, 0 },
            { NodeType.Tool, 0 }
        };
    }

    /// <summary>
    /// Gets or sets the number of nodes executed.
    /// </summary>
    public int NodesExecuted { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens used across all LLM calls.
    /// </summary>
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Gets or sets the total estimated cost of the execution.
    /// </summary>
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the execution.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets the counters for each node type executed.
    /// </summary>
    public Dictionary<NodeType, int> NodeTypeCounters { get; }

    /// <summary>
    /// Increments the count for a specific node type.
    /// </summary>
    /// <param name="nodeType">The node type to increment</param>
    public void IncrementNodeTypeCounter(NodeType nodeType)
    {
        if (NodeTypeCounters.ContainsKey(nodeType))
        {
            NodeTypeCounters[nodeType]++;
        }
    }

    /// <summary>
    /// Adds tokens to the total token count.
    /// </summary>
    /// <param name="tokens">The number of tokens to add</param>
    public void AddTokens(int tokens)
    {
        TotalTokensUsed += tokens;
    }

    /// <summary>
    /// Adds cost to the total estimated cost.
    /// </summary>
    /// <param name="cost">The cost to add</param>
    public void AddCost(decimal cost)
    {
        TotalEstimatedCost += cost;
    }
}
