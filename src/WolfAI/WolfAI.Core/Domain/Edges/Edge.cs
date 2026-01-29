using WolfAI.Core.Domain.Execution;

namespace WolfAI.Core.Domain.Edges;

/// <summary>
/// Represents a directed edge between two nodes in the graph.
/// </summary>
public class Edge
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Edge"/> class.
    /// </summary>
    public Edge(
        string id,
        string sourceNodeId,
        string targetNodeId,
        Func<IExecutionContext, bool>? routingFunction = null,
        int priority = 0,
        EdgeMetadata? metadata = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        SourceNodeId = sourceNodeId ?? throw new ArgumentNullException(nameof(sourceNodeId));
        TargetNodeId = targetNodeId ?? throw new ArgumentNullException(nameof(targetNodeId));
        RoutingFunction = routingFunction;
        Priority = priority;
        Metadata = metadata ?? new EdgeMetadata();
    }

    /// <summary>
    /// Gets the unique identifier for this edge.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the ID of the source node.
    /// </summary>
    public string SourceNodeId { get; }

    /// <summary>
    /// Gets the ID of the target node.
    /// </summary>
    public string TargetNodeId { get; }

    /// <summary>
    /// Gets the routing function that determines if this edge should be taken.
    /// If null, the edge is always active.
    /// </summary>
    public Func<IExecutionContext, bool>? RoutingFunction { get; }

    /// <summary>
    /// Gets the priority of this edge. Lower values are evaluated first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Gets the metadata associated with this edge.
    /// </summary>
    public EdgeMetadata Metadata { get; }

    /// <summary>
    /// Evaluates whether this edge should be taken based on the routing function.
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>True if the edge should be taken, false otherwise</returns>
    public bool Evaluate(IExecutionContext context)
    {
        if (RoutingFunction == null)
        {
            return true;
        }

        return RoutingFunction(context);
    }
}

