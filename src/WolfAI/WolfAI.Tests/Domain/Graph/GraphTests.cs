using System.Collections.ObjectModel;
using FluentAssertions;
using WolfAI.Tests.Fixtures;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Edges;
using GraphModel = WolfAI.Core.Domain.Graph.Graph;

namespace WolfAI.Tests.Domain.Graph;

public class GraphTests
{
    private Dictionary<string, Node> CreateTestNodes()
    {
        return new()
        {
            { "start", new TestNode("start", "Start Node", NodeType.Start) },
            { "middle", new TestNode("middle", "Middle Node", NodeType.AI) },
            { "end", new TestNode("end", "End Node", NodeType.End) }
        };
    }

    private List<Edge> CreateTestEdges()
    {
        return new()
        {
            new Edge("edge-1", "start", "middle"),
            new Edge("edge-2", "middle", "end")
        };
    }

    [Fact]
    public void Graph_Constructor_Sets_Properties()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();

        // Act
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Assert
        graph.Id.Should().Be("graph-1");
        graph.Name.Should().Be("Test Graph");
        graph.Nodes.Should().HaveCount(3);
        graph.Edges.Should().HaveCount(2);
        graph.EntryNodeId.Should().Be("start");
    }

    [Fact]
    public void Graph_Constructor_Throws_When_Id_Null()
    {
        // Act & Assert
        var act = () => new GraphModel(null!, "Test", new Dictionary<string, Node>(), new List<Edge>(), "start");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Graph_Constructor_Throws_When_Entry_Node_Not_Found()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();

        // Act & Assert
        var act = () => new GraphModel("graph-1", "Test Graph", nodes, edges, "nonexistent");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Entry node 'nonexistent' not found*");
    }

    [Fact]
    public void Graph_Constructor_Throws_When_Edge_Source_Node_Not_Found()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = new List<Edge>
        {
            new Edge("edge-1", "nonexistent", "middle")
        };

        // Act & Assert
        var act = () => new GraphModel("graph-1", "Test Graph", nodes, edges, "start");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Edge source node 'nonexistent' not found*");
    }

    [Fact]
    public void Graph_Constructor_Throws_When_Edge_Target_Node_Not_Found()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "nonexistent")
        };

        // Act & Assert
        var act = () => new GraphModel("graph-1", "Test Graph", nodes, edges, "start");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Edge target node 'nonexistent' not found*");
    }

    [Fact]
    public void Graph_Constructor_Throws_When_Orphaned_Nodes_Exist()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "middle", new TestNode("middle", "Middle") },
            { "orphan", new TestNode("orphan", "Orphan") },
            { "end", new TestNode("end", "End") }
        };
        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "middle"),
            new Edge("edge-2", "middle", "end")
        };

        // Act & Assert
        var act = () => new GraphModel("graph-1", "Test Graph", nodes, edges, "start");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*orphaned nodes*");
    }

    [Fact]
    public void Graph_EntryNode_Returns_Entry_Node()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var entryNode = graph.EntryNode;

        // Assert
        entryNode.Id.Should().Be("start");
        entryNode.Name.Should().Be("Start Node");
    }

    [Fact]
    public void Graph_GetOutgoingEdges_Returns_Edges_In_Priority_Order()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "node1", new TestNode("node1", "Node1") },
            { "node2", new TestNode("node2", "Node2") },
            { "node3", new TestNode("node3", "Node3") }
        };
        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "node1", priority: 5),
            new Edge("edge-2", "start", "node2", priority: 1),
            new Edge("edge-3", "start", "node3", priority: 3)
        };
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var outgoing = graph.GetOutgoingEdges("start");

        // Assert
        outgoing.Should().HaveCount(3);
        outgoing[0].Priority.Should().Be(1);
        outgoing[1].Priority.Should().Be(3);
        outgoing[2].Priority.Should().Be(5);
    }

    [Fact]
    public void Graph_GetOutgoingEdges_Returns_Empty_When_No_Edges()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var outgoing = graph.GetOutgoingEdges("end");

        // Assert
        outgoing.Should().BeEmpty();
    }

    [Fact]
    public void Graph_GetIncomingEdges_Returns_Edges()
    {
        // Arrange
        var nodes = new Dictionary<string, Node>
        {
            { "start", new TestNode("start", "Start") },
            { "middle", new TestNode("middle", "Middle") },
            { "node1", new TestNode("node1", "Node1") },
            { "node2", new TestNode("node2", "Node2") }
        };
        var edges = new List<Edge>
        {
            new Edge("edge-1", "start", "middle"),
            new Edge("edge-2", "node1", "middle"),
            new Edge("edge-3", "node2", "middle")
        };
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var incoming = graph.GetIncomingEdges("middle");

        // Assert
        incoming.Should().HaveCount(3);
    }

    [Fact]
    public void Graph_GetIncomingEdges_Returns_Empty_When_No_Edges()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act
        var incoming = graph.GetIncomingEdges("start");

        // Assert
        incoming.Should().BeEmpty();
    }

    [Fact]
    public void Graph_Nodes_Are_ReadOnly()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act & Assert
        graph.Nodes.Should().BeOfType<ReadOnlyDictionary<string, Node>>();
    }

    [Fact]
    public void Graph_Edges_Are_ReadOnly()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var graph = new GraphModel("graph-1", "Test Graph", nodes, edges, "start");

        // Act & Assert
        graph.Edges.Should().BeOfType<ReadOnlyCollection<Edge>>();
    }
}
