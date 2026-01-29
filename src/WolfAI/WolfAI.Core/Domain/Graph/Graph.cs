using System.Collections.ObjectModel;
using WolfAI.Core.Domain.Edges;
using WolfAI.Core.Domain.Nodes;

namespace WolfAI.Core.Domain.Graph;

/// <summary>
/// Represents a directed acyclic graph (DAG) for workflow execution.
/// </summary>
public class Graph
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Graph"/> class.
    /// </summary>
    public Graph(
        string id,
        string name,
        IReadOnlyDictionary<string, Node> nodes,
        IReadOnlyList<Edge> edges,
        string entryNodeId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (edges == null) throw new ArgumentNullException(nameof(edges));
        Nodes = new ReadOnlyDictionary<string, Node>(
            new Dictionary<string, Node>(nodes));
        Edges = new ReadOnlyCollection<Edge>(edges.ToList());
        EntryNodeId = entryNodeId ?? throw new ArgumentNullException(nameof(entryNodeId));

        ValidateGraph();
    }

    /// <summary>
    /// Gets the unique identifier for this graph.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of this graph.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets all nodes in the graph, indexed by ID.
    /// </summary>
    public IReadOnlyDictionary<string, Node> Nodes { get; }

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IReadOnlyList<Edge> Edges { get; }

    /// <summary>
    /// Gets the ID of the entry point node.
    /// </summary>
    public string EntryNodeId { get; }

    /// <summary>
    /// Gets the entry node.
    /// </summary>
    public Node EntryNode => Nodes[EntryNodeId];

    /// <summary>
    /// Gets the outgoing edges from a node.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    /// <returns>Collection of outgoing edges</returns>
    public IReadOnlyList<Edge> GetOutgoingEdges(string nodeId)
    {
        return Edges
            .Where(e => e.SourceNodeId == nodeId)
            .OrderBy(e => e.Priority)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the incoming edges to a node.
    /// </summary>
    /// <param name="nodeId">The node ID</param>
    /// <returns>Collection of incoming edges</returns>
    public IReadOnlyList<Edge> GetIncomingEdges(string nodeId)
    {
        return Edges
            .Where(e => e.TargetNodeId == nodeId)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Validates the graph structure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the graph structure is invalid</exception>
    private void ValidateGraph()
    {
        // Verify entry node exists
        if (!Nodes.ContainsKey(EntryNodeId))
        {
            throw new InvalidOperationException($"Entry node '{EntryNodeId}' not found in graph.");
        }

        // Verify all edge source and target nodes exist
        foreach (var edge in Edges)
        {
            if (!Nodes.ContainsKey(edge.SourceNodeId))
            {
                throw new InvalidOperationException(
                    $"Edge source node '{edge.SourceNodeId}' not found in graph.");
            }

            if (!Nodes.ContainsKey(edge.TargetNodeId))
            {
                throw new InvalidOperationException(
                    $"Edge target node '{edge.TargetNodeId}' not found in graph.");
            }
        }

        // Verify no orphaned nodes (all non-entry nodes should have either incoming or outgoing edges)
        var connectedNodes = new HashSet<string> { EntryNodeId };

        foreach (var edge in Edges)
        {
            connectedNodes.Add(edge.SourceNodeId);
            connectedNodes.Add(edge.TargetNodeId);
        }

        var orphanedNodes = Nodes.Keys.Except(connectedNodes).ToList();
        if (orphanedNodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Graph contains orphaned nodes: {string.Join(", ", orphanedNodes)}");
        }
    }
}
