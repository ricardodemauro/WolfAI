using System.Collections.ObjectModel;
using WolfAI.Core.Domain.Edges;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;

namespace WolfAI.Core.Domain.Graph;

/// <summary>
/// Fluent builder for constructing directed acyclic graphs with nodes and edges.
/// </summary>
/// <remarks>
/// The GraphBuilder provides a convenient API for constructing graphs while ensuring
/// proper structure and validation. It automatically manages StartNode and EndNode creation.
/// </remarks>
/// <example>
/// <code>
/// var graph = new GraphBuilder("MyWorkflow")
///     .AddStartNode()
///     .AddAINode("ai-1", "LLM Classifier", async (context, ct) =>
///     {
///         // User execution logic
///         return NodeResult.SuccessResult(output: "classified");
///     })
///     .AddEdge("start", "ai-1")
///     .AddEdge("ai-1", "end")
///     .build();
/// </code>
/// </example>
public class GraphBuilder
{
    private readonly string _graphId;
    private readonly string _graphName;
    private readonly Dictionary<string, Node> _nodes;
    private readonly List<Edge> _edges;
    private string? _startNodeId;
    private string? _endNodeId;
    private int _edgeCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphBuilder"/> class.
    /// </summary>
    /// <param name="graphName">Human-readable name for the graph</param>
    /// <exception cref="ArgumentNullException">Thrown when graphName is null or empty</exception>
    public GraphBuilder(string graphName)
    {
        if (string.IsNullOrWhiteSpace(graphName))
        {
            throw new ArgumentNullException(nameof(graphName));
        }

        _graphName = graphName;
        _graphId = Guid.NewGuid().ToString();
        _nodes = new Dictionary<string, Node>();
        _edges = new List<Edge>();
        _edgeCounter = 0;
    }

    /// <summary>
    /// Adds a StartNode to the graph as the entry point.
    /// Can only be called once. Subsequent calls will throw.
    /// </summary>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown if StartNode already added</exception>
    public GraphBuilder AddStartNode()
    {
        if (_startNodeId != null)
        {
            throw new InvalidOperationException("A StartNode has already been added to this graph.");
        }

        var startNode = new StartNode("start", "Start");
        _startNodeId = startNode.Id;
        _nodes[startNode.Id] = startNode;

        return this;
    }

    /// <summary>
    /// Adds an EndNode to the graph as the exit point.
    /// Can only be called once. Subsequent calls will throw.
    /// </summary>
    /// <param name="id">Unique identifier for the end node (optional)</param>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown if EndNode already added</exception>
    public GraphBuilder AddEndNode(string? id = null)
    {
        if (_endNodeId != null)
        {
            throw new InvalidOperationException("An EndNode has already been added to this graph.");
        }

        var endNodeId = id ?? "end";
        var endNode = new EndNode(endNodeId, "End");
        _endNodeId = endNode.Id;
        _nodes[endNode.Id] = endNode;

        return this;
    }

    /// <summary>
    /// Adds an AINode to the graph with user-defined execution logic.
    /// </summary>
    /// <param name="id">Unique identifier for the node</param>
    /// <param name="name">Human-readable name for the node</param>
    /// <param name="executionLogic">The async execution logic for the node</param>
    /// <param name="configure">Optional configuration action for the node</param>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when id, name, or executionLogic is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if node with same id already exists</exception>
    public GraphBuilder AddAINode(
        string id,
        string name,
        Func<WolfAI.Core.Domain.Execution.IExecutionContext, CancellationToken, Task<NodeResult>> executionLogic,
        Action<AINodeBuilder>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (executionLogic == null)
        {
            throw new ArgumentNullException(nameof(executionLogic));
        }

        if (_nodes.ContainsKey(id))
        {
            throw new InvalidOperationException($"A node with ID '{id}' already exists in this graph.");
        }

        var aiNode = new AINode(id, name)
        {
            ExecutionLogic = executionLogic
        };

        // Apply optional configuration
        if (configure != null)
        {
            var nodeBuilder = new AINodeBuilder(aiNode);
            configure(nodeBuilder);
        }

        _nodes[id] = aiNode;

        return this;
    }

    /// <summary>
    /// Adds a custom node to the graph.
    /// </summary>
    /// <param name="node">The node to add</param>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if node with same id already exists</exception>
    public GraphBuilder AddNode(Node node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_nodes.ContainsKey(node.Id))
        {
            throw new InvalidOperationException($"A node with ID '{node.Id}' already exists in this graph.");
        }

        _nodes[node.Id] = node;

        return this;
    }

    /// <summary>
    /// Adds an edge connecting two nodes with optional routing logic.
    /// </summary>
    /// <param name="sourceNodeId">ID of the source node</param>
    /// <param name="targetNodeId">ID of the target node</param>
    /// <param name="routingFunction">Optional function to determine if edge should be taken</param>
    /// <param name="priority">Priority for edge evaluation (lower evaluated first)</param>
    /// <param name="id">Optional edge identifier (auto-generated if not provided)</param>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceNodeId or targetNodeId is null</exception>
    public GraphBuilder AddEdge(
        string sourceNodeId,
        string targetNodeId,
        Func<WolfAI.Core.Domain.Execution.IExecutionContext, bool>? routingFunction = null,
        int priority = 0,
        string? id = null)
    {
        if (string.IsNullOrWhiteSpace(sourceNodeId))
        {
            throw new ArgumentNullException(nameof(sourceNodeId));
        }

        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            throw new ArgumentNullException(nameof(targetNodeId));
        }

        var edgeId = id ?? $"edge_{_edgeCounter++}";
        var edge = new Edge(
            id: edgeId,
            sourceNodeId: sourceNodeId,
            targetNodeId: targetNodeId,
            routingFunction: routingFunction,
            priority: priority);

        _edges.Add(edge);

        return this;
    }

    /// <summary>
    /// Builds and validates the graph.
    /// </summary>
    /// <returns>A validated, immutable Graph instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if graph structure is invalid</exception>
    public Graph Build()
    {
        ValidateGraph();

        return new Graph(
            id: _graphId,
            name: _graphName,
            nodes: new ReadOnlyDictionary<string, Node>(_nodes),
            edges: _edges.AsReadOnly(),
            entryNodeId: _startNodeId!);
    }

    /// <summary>
    /// Validates the graph structure before building.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if graph is invalid</exception>
    private void ValidateGraph()
    {
        // Verify StartNode was added
        if (_startNodeId == null)
        {
            throw new InvalidOperationException("Graph must have a StartNode. Call AddStartNode() before building.");
        }

        // Verify EndNode was added
        if (_endNodeId == null)
        {
            throw new InvalidOperationException("Graph must have an EndNode. Call AddEndNode() before building.");
        }

        // Verify at least 2 nodes (start + end)
        if (_nodes.Count < 2)
        {
            throw new InvalidOperationException("Graph must have at least a StartNode and an EndNode.");
        }

        // Verify all edge nodes exist
        foreach (var edge in _edges)
        {
            if (!_nodes.ContainsKey(edge.SourceNodeId))
            {
                throw new InvalidOperationException($"Edge references non-existent source node: '{edge.SourceNodeId}'");
            }

            if (!_nodes.ContainsKey(edge.TargetNodeId))
            {
                throw new InvalidOperationException($"Edge references non-existent target node: '{edge.TargetNodeId}'");
            }
        }

        // Verify EndNode has no outgoing edges
        var endNodeOutgoing = _edges.Where(e => e.SourceNodeId == _endNodeId).ToList();
        if (endNodeOutgoing.Count > 0)
        {
            throw new InvalidOperationException(
                $"EndNode '{_endNodeId}' cannot have outgoing edges. Found {endNodeOutgoing.Count} outgoing edge(s).");
        }

        // Verify no orphaned nodes
        var connectedNodes = new HashSet<string> { _startNodeId };
        foreach (var edge in _edges)
        {
            connectedNodes.Add(edge.SourceNodeId);
            connectedNodes.Add(edge.TargetNodeId);
        }

        var orphanedNodes = _nodes.Keys.Except(connectedNodes).ToList();
        if (orphanedNodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Graph contains orphaned (unconnected) nodes: {string.Join(", ", orphanedNodes)}");
        }
    }

    /// <summary>
    /// Gets the graph ID that will be assigned to the built graph.
    /// </summary>
    public string GraphId => _graphId;

    /// <summary>
    /// Gets the number of nodes added to the builder.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of edges added to the builder.
    /// </summary>
    public int EdgeCount => _edges.Count;
}
