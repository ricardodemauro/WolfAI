using System.Collections.ObjectModel;
using WolfAI.Core.Domain.Results;
using WolfAI.Core.Domain.Execution;

namespace WolfAI.Core.Domain.Nodes;

/// <summary>
/// Abstract base class for all nodes in the graph.
/// </summary>
public abstract class Node
{

    /// <summary>
    /// Initializes a new instance of the <see cref="Node"/> class.
    /// </summary>
    protected Node(string id, string name)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Middleware = [];
    }

    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of this node.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of this node.
    /// </summary>
    public abstract NodeType NodeType { get; }

    /// <summary>
    /// Gets the collection of middleware for this node.
    /// </summary>
    public ICollection<INodeMiddleware> Middleware { get; }

    /// <summary>
    /// Executes the node asynchronously.
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of node execution</returns>
    public abstract Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for node middleware.
/// </summary>
public interface INodeMiddleware
{
}
