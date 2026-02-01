using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Core.Domain.Graph;

/// <summary>
/// Helper builder for configuring individual AI nodes with optional settings.
/// </summary>
/// <remarks>
/// AINodeBuilder provides a fluent interface for configuring node-specific options
/// such as middleware and other settings that may be extended in future phases.
/// </remarks>
public class AINodeBuilder
{
    private readonly AINode _node;

    /// <summary>
    /// Initializes a new instance of the <see cref="AINodeBuilder"/> class.
    /// </summary>
    /// <param name="node">The AINode to configure</param>
    internal AINodeBuilder(AINode node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    /// Gets the underlying AINode being configured.
    /// </summary>
    public AINode Node => _node;

    /// <summary>
    /// Adds middleware to the node.
    /// </summary>
    /// <param name="middleware">The middleware to add</param>
    /// <returns>This builder instance for method chaining</returns>
    public AINodeBuilder WithMiddleware(INodeMiddleware middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _node.Middleware.Add(middleware);

        return this;
    }
}
