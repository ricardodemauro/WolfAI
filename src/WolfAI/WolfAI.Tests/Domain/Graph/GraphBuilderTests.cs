using FluentAssertions;
using WolfAI.Core.Domain.Graph;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Results;
using WolfAI.Core.Domain.Execution;

namespace WolfAI.Tests.Domain.Graph;

public class GraphBuilderTests
{
    private Func<IExecutionContext, CancellationToken, Task<NodeResult>> CreateDummyExecutionLogic()
    {
        return async (context, ct) => NodeResult.SuccessResult(output: "test");
    }

    [Fact]
    public void GraphBuilder_Constructor_Sets_GraphName()
    {
        // Act
        var builder = new GraphBuilder("TestWorkflow");

        // Assert
        builder.NodeCount.Should().Be(0);
        builder.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void GraphBuilder_Constructor_Throws_When_GraphName_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_Constructor_Throws_When_GraphName_Empty()
    {
        // Act & Assert
        var act = () => new GraphBuilder("");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddStartNode_Adds_StartNode()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode();

        // Assert
        builder.NodeCount.Should().Be(1);
    }

    [Fact]
    public void GraphBuilder_AddStartNode_Throws_When_Called_Twice()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddStartNode();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StartNode has already been added*");
    }

    [Fact]
    public void GraphBuilder_AddEndNode_Adds_EndNode()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddEndNode();

        // Assert
        builder.NodeCount.Should().Be(2);
    }

    [Fact]
    public void GraphBuilder_AddEndNode_With_Custom_Id()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddEndNode("custom-end");

        // Assert
        builder.NodeCount.Should().Be(2);
    }

    [Fact]
    public void GraphBuilder_AddEndNode_Throws_When_Called_Twice()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddEndNode()
            .AddEndNode();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EndNode has already been added*");
    }

    [Fact]
    public void GraphBuilder_AddAINode_Adds_Node()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode();

        // Assert
        builder.NodeCount.Should().Be(3);
    }

    [Fact]
    public void GraphBuilder_AddAINode_Throws_When_Id_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddAINode(null!, "Process", CreateDummyExecutionLogic());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddAINode_Throws_When_Name_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddAINode("ai-1", null!, CreateDummyExecutionLogic());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddAINode_Throws_When_ExecutionLogic_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddAINode("ai-1", "Process", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddAINode_Throws_When_Duplicate_Id()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddAINode("ai-1", "Process1", CreateDummyExecutionLogic())
            .AddAINode("ai-1", "Process2", CreateDummyExecutionLogic());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*node with ID 'ai-1' already exists*");
    }

    [Fact]
    public void GraphBuilder_AddAINode_With_Configuration()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic(), nodeBuilder =>
            {
                // Configuration can happen here
            })
            .AddEndNode();

        // Assert
        builder.NodeCount.Should().Be(3);
    }

    [Fact]
    public void GraphBuilder_AddNode_Adds_Custom_Node()
    {
        // Arrange
        var customNode = new AINode("custom", "Custom") { ExecutionLogic = CreateDummyExecutionLogic() };

        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddNode(customNode)
            .AddEndNode();

        // Assert
        builder.NodeCount.Should().Be(3);
    }

    [Fact]
    public void GraphBuilder_AddNode_Throws_When_Node_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test").AddNode(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddNode_Throws_On_Duplicate_Id()
    {
        // Arrange
        var node1 = new AINode("ai-1", "First") { ExecutionLogic = CreateDummyExecutionLogic() };
        var node2 = new AINode("ai-1", "Second") { ExecutionLogic = CreateDummyExecutionLogic() };

        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddNode(node1)
            .AddNode(node2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ID 'ai-1' already exists*");
    }

    [Fact]
    public void GraphBuilder_AddEdge_Adds_Edge()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "end");

        // Assert
        builder.EdgeCount.Should().Be(2);
    }

    [Fact]
    public void GraphBuilder_AddEdge_Throws_When_SourceNodeId_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test").AddEdge(null!, "target");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddEdge_Throws_When_TargetNodeId_Null()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test").AddEdge("source", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GraphBuilder_AddEdge_With_RoutingFunction()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddAINode("ai-2", "Process2", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "ai-2", ctx =>
            {
                if (ctx.GlobalVariables.TryGetValue("success", out var val))
                {
                    return (bool?)val == true;
                }
                return false;
            })
            .AddEdge("ai-1", "end");

        // Assert
        builder.EdgeCount.Should().Be(3);
    }

    [Fact]
    public void GraphBuilder_AddEdge_With_Priority()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1", priority: 5);

        // Assert
        builder.EdgeCount.Should().Be(1);
    }

    [Fact]
    public void GraphBuilder_AddEdge_With_CustomId()
    {
        // Act
        var builder = new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1", id: "edge-start-to-ai");

        // Assert
        builder.EdgeCount.Should().Be(1);
    }

    [Fact]
    public void GraphBuilder_Build_Returns_Graph()
    {
        // Act
        var graph = new GraphBuilder("TestWorkflow")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "end")
            .Build();

        // Assert
        graph.Should().NotBeNull();
        graph.Name.Should().Be("TestWorkflow");
        graph.Nodes.Should().HaveCount(3);
        graph.Edges.Should().HaveCount(2);
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_No_StartNode()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*StartNode*");
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_No_EndNode()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EndNode*");
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_Edge_References_NonExistent_Source()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddEndNode()
            .AddEdge("nonexistent", "end")
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-existent source node*");
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_Edge_References_NonExistent_Target()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddEndNode()
            .AddEdge("start", "nonexistent")
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-existent target node*");
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_EndNode_Has_Outgoing_Edges()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("end", "ai-1")  // EndNode cannot have outgoing edges
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EndNode*cannot have outgoing edges*");
    }

    [Fact]
    public void GraphBuilder_Build_Throws_When_Orphaned_Nodes_Exist()
    {
        // Act & Assert
        var act = () => new GraphBuilder("Test")
            .AddStartNode()
            .AddAINode("ai-1", "Process", CreateDummyExecutionLogic())
            .AddAINode("orphan", "Orphan", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "end")
            .Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*orphaned*");
    }

    [Fact]
    public void GraphBuilder_Fluent_Chaining()
    {
        // Act & Assert
        var graph = new GraphBuilder("ChainedWorkflow")
            .AddStartNode()
            .AddAINode("ai-1", "Step1", CreateDummyExecutionLogic())
            .AddAINode("ai-2", "Step2", CreateDummyExecutionLogic())
            .AddAINode("ai-3", "Step3", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "ai-2")
            .AddEdge("ai-2", "ai-3")
            .AddEdge("ai-3", "end")
            .Build();

        // Assert
        graph.Nodes.Should().HaveCount(5); // start, ai-1, ai-2, ai-3, end
        graph.Edges.Should().HaveCount(4);
    }

    [Fact]
    public void GraphBuilder_Multiple_Outgoing_Edges_With_Routing()
    {
        // Arrange
        var successEdge = (IExecutionContext ctx) =>
        {
            if (ctx.GlobalVariables.TryGetValue("result", out var val))
            {
                return (string?)val == "success";
            }
            return false;
        };
        var failureEdge = (IExecutionContext ctx) =>
        {
            if (ctx.GlobalVariables.TryGetValue("result", out var val))
            {
                return (string?)val != "success";
            }
            return true;
        };

        // Act
        var graph = new GraphBuilder("ConditionalWorkflow")
            .AddStartNode()
            .AddAINode("ai-1", "Decide", CreateDummyExecutionLogic())
            .AddAINode("success-node", "Success", CreateDummyExecutionLogic())
            .AddAINode("failure-node", "Failure", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "ai-1")
            .AddEdge("ai-1", "success-node", successEdge, priority: 0)
            .AddEdge("ai-1", "failure-node", failureEdge, priority: 1)
            .AddEdge("success-node", "end")
            .AddEdge("failure-node", "end")
            .Build();

        // Assert
        graph.Nodes.Should().HaveCount(5);
        graph.Edges.Should().HaveCount(5);
        var decisionNodeEdges = graph.GetOutgoingEdges("ai-1");
        decisionNodeEdges.Should().HaveCount(2);
        decisionNodeEdges.Should().HaveElementAt(0, decisionNodeEdges[0]);
    }

    [Fact]
    public void GraphBuilder_Complex_Workflow()
    {
        // Act
        var graph = new GraphBuilder("ComplexWorkflow")
            .AddStartNode()
            .AddAINode("validate", "Validate Input", CreateDummyExecutionLogic())
            .AddAINode("enrich", "Enrich Data", CreateDummyExecutionLogic())
            .AddAINode("classify", "Classify", CreateDummyExecutionLogic())
            .AddAINode("generate", "Generate Response", CreateDummyExecutionLogic())
            .AddEndNode()
            .AddEdge("start", "validate")
            .AddEdge("validate", "enrich", ctx =>
            {
                if (ctx.GlobalVariables.TryGetValue("valid", out var val))
                {
                    return (bool?)val == true;
                }
                return false;
            })
            .AddEdge("enrich", "classify")
            .AddEdge("classify", "generate")
            .AddEdge("generate", "end")
            .Build();

        // Assert
        graph.Should().NotBeNull();
        graph.Name.Should().Be("ComplexWorkflow");
        graph.Nodes.Should().HaveCount(6); // start + 4 AI nodes + end
        graph.Edges.Should().HaveCount(5);
        graph.EntryNode.NodeType.Should().Be(NodeType.Start);
    }

    [Fact]
    public void GraphBuilder_GraphId_IsUnique()
    {
        // Act
        var builder1 = new GraphBuilder("Graph1");
        var builder2 = new GraphBuilder("Graph2");

        // Assert
        builder1.GraphId.Should().NotBe(builder2.GraphId);
    }

    [Fact]
    public void GraphBuilder_NodeCount_And_EdgeCount_Updated()
    {
        // Act
        var builder = new GraphBuilder("Test");
        builder.NodeCount.Should().Be(0);
        builder.EdgeCount.Should().Be(0);

        builder.AddStartNode();
        builder.NodeCount.Should().Be(1);

        builder.AddAINode("ai-1", "Process", CreateDummyExecutionLogic());
        builder.NodeCount.Should().Be(2);

        builder.AddEndNode();
        builder.NodeCount.Should().Be(3);

        builder.AddEdge("start", "ai-1");
        builder.EdgeCount.Should().Be(1);

        builder.AddEdge("ai-1", "end");
        builder.EdgeCount.Should().Be(2);
    }
}
