using Microsoft.Extensions.Logging;
using WolfAI.Core.Domain.Edges;
using WolfAI.Core.Domain.Graph;
using WolfAI.Core.Domain.Nodes;
using WolfAI.Core.Domain.Execution;
using WolfAI.Core.Domain.Results;
using ExecutionContext = WolfAI.Core.Domain.Execution.ExecutionContext;

namespace WolfAI.Samples;

/// <summary>
/// Simple sample demonstrating WolfAI.Core APIs.
/// Shows how to build a graph, create execution context, and traverse nodes.
/// This sample demonstrates:
/// 1. Graph construction with nodes and edges
/// 2. ExecutionContext creation with immutable updates
/// 3. Manual graph traversal with routing
/// 4. State management via snapshots
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Setup logging
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("WolfAISample");

        Console.WriteLine("=== WolfAI Core API Sample ===\n");

        // Step 1: Create custom nodes for demonstration
        Console.WriteLine("Step 1: Creating nodes...");
        var startNode = new SimpleStartNode("start_1", "Start");
        var processNode = new ProcessingNode("process_1", "Process Input");
        var decisionNode = new DecisionNode("decision_1", "Decision Branch");
        var successEndNode = new SimpleEndNode("end_success", "Success End");
        var failEndNode = new SimpleEndNode("end_fail", "Failure End");
        Console.WriteLine("  Created: StartNode, ProcessingNode, DecisionNode, 2x EndNodes\n");

        // Step 2: Create edges to connect nodes
        Console.WriteLine("Step 2: Creating edges and building graph...");
        var nodes = new Dictionary<string, Node>
        {
            { startNode.Id, startNode },
            { processNode.Id, processNode },
            { decisionNode.Id, decisionNode },
            { successEndNode.Id, successEndNode },
            { failEndNode.Id, failEndNode }
        };

        var edges = new List<Edge>
        {
            // Start -> Process: Always taken
            new Edge(
                id: "edge_start_process",
                sourceNodeId: startNode.Id,
                targetNodeId: processNode.Id,
                routingFunction: null, // Always true
                priority: 0),

            // Process -> Decision: Always taken
            new Edge(
                id: "edge_process_decision",
                sourceNodeId: processNode.Id,
                targetNodeId: decisionNode.Id,
                routingFunction: null,
                priority: 0),

            // Decision -> Success: If processed_success = true
            new Edge(
                id: "edge_decision_success",
                sourceNodeId: decisionNode.Id,
                targetNodeId: successEndNode.Id,
                routingFunction: ctx => ctx.GlobalVariables.TryGetValue("processed_success", out var val) && (bool?)val == true,
                priority: 0),

            // Decision -> Failure: If processed_success != true
            new Edge(
                id: "edge_decision_fail",
                sourceNodeId: decisionNode.Id,
                targetNodeId: failEndNode.Id,
                routingFunction: ctx => ctx.GlobalVariables.TryGetValue("processed_success", out var val) && (bool?)val != true,
                priority: 1)
        };

        var graph = new Graph(
            id: "sample_graph_1",
            name: "Sample Graph with Routing",
            nodes: nodes,
            edges: edges,
            entryNodeId: startNode.Id);

        Console.WriteLine($"  Created graph: {graph.Name} (ID: {graph.Id})");
        Console.WriteLine($"  Nodes: {string.Join(", ", graph.Nodes.Keys)}");
        Console.WriteLine($"  Edges: {edges.Count}\n");

        // Step 3: Create execution context
        Console.WriteLine("Step 3: Creating execution context...");
        var initialVariables = new Dictionary<string, object?>
        {
            { "input", "Hello, WolfAI! Testing graph execution." }
        };

        var context = new ExecutionContext(
            executionId: $"exec_{Guid.NewGuid():N}",
            threadId: $"thread_{Guid.NewGuid():N}",
            graphId: graph.Id,
            currentNodeId: graph.EntryNodeId,
            globalVariables: initialVariables,
            logger: logger);

        Console.WriteLine($"  ExecutionId: {context.ExecutionId}");
        Console.WriteLine($"  Input: {context.GlobalVariables["input"]}\n");

        // Step 4: Execute the graph manually (step through nodes)
        Console.WriteLine("Step 4: Manually traversing graph...\n");

        var currentContext = context;
        var visited = new HashSet<string>();
        int step = 0;

        while (step < 10) // Safety limit
        {
            step++;
            var currentNode = graph.Nodes[currentContext.CurrentNodeId];
            
            Console.WriteLine($"  [{step}] Executing node: {currentNode.Name} (ID: {currentNode.Id}, Type: {currentNode.NodeType})");
            
            // Check if we've already visited this node (cycle detection)
            if (visited.Contains(currentNode.Id) && currentNode.NodeType != NodeType.Start)
            {
                Console.WriteLine($"  Cycle detected at {currentNode.Name}, stopping traversal");
                break;
            }
            visited.Add(currentNode.Id);

            // Execute the node
            var result = await currentNode.ExecuteAsync(currentContext, CancellationToken.None);

            // Update context with result
            var variablesToAdd = result.Variables?.Count > 0 
                ? new Dictionary<string, object?>(result.Variables) 
                : null;
            currentContext = currentContext.WithUpdates(
                newMessages: result.Messages,
                newVariables: variablesToAdd,
                recordNodeExecution: currentNode.Id);

            Console.WriteLine($"      Success: {result.Success}");
            Console.WriteLine($"      Duration: {result.Duration.TotalMilliseconds:F2}ms");

            if (!result.Success)
            {
                Console.WriteLine($"      Error: {result.Error}");
                break;
            }

            // Check if we've reached an end node
            if (currentNode.NodeType == NodeType.End)
            {
                Console.WriteLine("  End node reached, execution complete.\n");
                break;
            }

            // Get outgoing edges and find the next node
            var outgoingEdges = graph.GetOutgoingEdges(currentNode.Id);
            
            if (outgoingEdges.Count == 0)
            {
                Console.WriteLine("  No outgoing edges found, stopping traversal");
                break;
            }

            Console.WriteLine($"      Checking {outgoingEdges.Count} outgoing edge(s)...");

            // Find the first edge that evaluates to true
            Edge? nextEdge = null;
            foreach (var edge in outgoingEdges)
            {
                if (edge.Evaluate(currentContext))
                {
                    Console.WriteLine($"        Edge '{edge.Id}' evaluated to true (Priority: {edge.Priority})");
                    nextEdge = edge;
                    break;
                }
                else
                {
                    Console.WriteLine($"        Edge '{edge.Id}' evaluated to false");
                }
            }

            if (nextEdge == null)
            {
                Console.WriteLine("  No edges evaluated to true, stopping traversal");
                break;
            }

            // Move to next node
            currentContext = currentContext.WithUpdates(newNodeId: nextEdge.TargetNodeId);
            Console.WriteLine($"      Moving to node: {graph.Nodes[nextEdge.TargetNodeId].Name}\n");
        }

        // Step 5: Print execution summary
        Console.WriteLine("\nStep 5: Execution Summary");
        Console.WriteLine("============================");
        Console.WriteLine($"  Execution ID: {currentContext.ExecutionId}");
        Console.WriteLine($"  Graph: {graph.Name}");
        Console.WriteLine($"  Nodes visited: {string.Join(" -> ", currentContext.NodeExecutionHistory)}");
        Console.WriteLine($"  Total messages produced: {currentContext.Messages.Count}");
        Console.WriteLine($"  Global variables: {string.Join(", ", currentContext.GlobalVariables.Keys)}");
        Console.WriteLine($"  Elapsed time: {currentContext.Elapsed.TotalMilliseconds:F2}ms");

        // Create and display snapshot
        var snapshot = currentContext.CreateSnapshot();
        Console.WriteLine("\n  Context Snapshot:");
        Console.WriteLine($"    Execution ID: {snapshot.ExecutionId}");
        Console.WriteLine($"    Current Node: {snapshot.CurrentNodeId}");
        Console.WriteLine($"    History: {string.Join(" -> ", snapshot.NodeExecutionHistory)}");
        Console.WriteLine($"    Final Variables: {string.Join(", ", snapshot.GlobalVariables.Keys)}");

        Console.WriteLine("\n=== Sample Complete ===");
    }
}

/// <summary>
/// Simple start node for demonstration.
/// </summary>
public class SimpleStartNode : Node
{
    public SimpleStartNode(string id, string name) : base(id, name) { }

    public override NodeType NodeType => NodeType.Start;

    public override Task<NodeResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            if (!context.GlobalVariables.TryGetValue("input", out var inputValue))
            {
                return Task.FromResult(NodeResult.FailureResult(
                    error: "No input in GlobalVariables",
                    duration: DateTime.UtcNow - startTime));
            }

            var input = inputValue?.ToString() ?? "";
            return Task.FromResult(NodeResult.SuccessResult(
                output: input,
                variables: new Dictionary<string, object?> { { "flow_stage", "started" } },
                duration: DateTime.UtcNow - startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeResult.FailureResult(error: ex.Message, duration: DateTime.UtcNow - startTime));
        }
    }
}

/// <summary>
/// Custom processing node for demonstration.
/// </summary>
public class ProcessingNode : Node
{
    public ProcessingNode(string id, string name) : base(id, name) { }

    public override NodeType NodeType => NodeType.AI;

    public override Task<NodeResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var inputValue = context.GlobalVariables.TryGetValue("input", out var val) 
                ? val?.ToString() ?? "No input" 
                : "No input";

            var processedValue = $"Processed: [{inputValue.ToUpper()}]";

            var newVariables = new Dictionary<string, object?>
            {
                { "processed_output", processedValue },
                { "processed_success", true }, // Indicate success for routing
                { "processing_timestamp", DateTime.UtcNow.ToString("O") }
            };

            return Task.FromResult(NodeResult.SuccessResult(
                output: processedValue,
                variables: newVariables,
                duration: DateTime.UtcNow - startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeResult.FailureResult(error: ex.Message, duration: DateTime.UtcNow - startTime));
        }
    }
}

/// <summary>
/// Decision node that demonstrates routing logic.
/// </summary>
public class DecisionNode : Node
{
    public DecisionNode(string id, string name) : base(id, name) { }

    public override NodeType NodeType => NodeType.AI;

    public override Task<NodeResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var decision = context.GlobalVariables.TryGetValue("processed_success", out var val) && (bool?)val == true
                ? "Success path"
                : "Failure path";

            var newVariables = new Dictionary<string, object?>
            {
                { "decision_result", decision }
            };

            return Task.FromResult(NodeResult.SuccessResult(
                output: decision,
                variables: newVariables,
                duration: DateTime.UtcNow - startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeResult.FailureResult(error: ex.Message, duration: DateTime.UtcNow - startTime));
        }
    }
}

/// <summary>
/// Simple end node for demonstration.
/// </summary>
public class SimpleEndNode : Node
{
    public SimpleEndNode(string id, string name) : base(id, name) { }

    public override NodeType NodeType => NodeType.End;

    public override Task<NodeResult> ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var finalOutput = $"Execution ended at {Name}";
            return Task.FromResult(NodeResult.SuccessResult(
                output: finalOutput,
                duration: DateTime.UtcNow - startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeResult.FailureResult(error: ex.Message, duration: DateTime.UtcNow - startTime));
        }
    }
}
