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

        // Step 1 & 2: Build graph using GraphBuilder
        Console.WriteLine("Step 1: Building graph with GraphBuilder...");
        var graph = new GraphBuilder("Sample Graph with Routing")
            .AddStartNode()
            .AddAINode("process_1", "Process Input", async (context, ct) =>
            {
                var startTime = DateTime.UtcNow;
                var inputValue = context.GlobalVariables.TryGetValue("input", out var val) 
                    ? val?.ToString() ?? "No input" 
                    : "No input";
                var processedValue = $"Processed: [{inputValue.ToUpper()}]";
                var newVariables = new Dictionary<string, object?>
                {
                    { "processed_output", processedValue },
                    { "processed_success", true },
                    { "processing_timestamp", DateTime.UtcNow.ToString("O") }
                };
                return NodeResult.SuccessResult(
                    output: processedValue,
                    variables: newVariables,
                    duration: DateTime.UtcNow - startTime);
            })
            .AddAINode("decision_1", "Decision Branch", async (context, ct) =>
            {
                var startTime = DateTime.UtcNow;
                var decision = context.GlobalVariables.TryGetValue("processed_success", out var val) && (bool?)val == true
                    ? "Success path"
                    : "Failure path";
                var newVariables = new Dictionary<string, object?>
                {
                    { "decision_result", decision }
                };
                return NodeResult.SuccessResult(
                    output: decision,
                    variables: newVariables,
                    duration: DateTime.UtcNow - startTime);
            })
            .AddAINode("end_success", "Success End", async (context, ct) =>
            {
                var startTime = DateTime.UtcNow;
                var finalOutput = $"Execution ended at Success End";
                return NodeResult.SuccessResult(
                    output: finalOutput,
                    duration: DateTime.UtcNow - startTime);
            })
            .AddAINode("end_fail", "Failure End", async (context, ct) =>
            {
                var startTime = DateTime.UtcNow;
                var finalOutput = $"Execution ended at Failure End";
                return NodeResult.SuccessResult(
                    output: finalOutput,
                    duration: DateTime.UtcNow - startTime);
            })
            .AddEdge("start", "process_1")
            .AddEdge("process_1", "decision_1")
            .AddEdge("decision_1", "end_success", ctx =>
            {
                if (ctx.GlobalVariables.TryGetValue("processed_success", out var val))
                {
                    return (bool?)val == true;
                }
                return false;
            }, priority: 0)
            .AddEdge("decision_1", "end_fail", ctx =>
            {
                if (ctx.GlobalVariables.TryGetValue("processed_success", out var val))
                {
                    return (bool?)val != true;
                }
                return true;
            }, priority: 1)
            .Build();

        Console.WriteLine($"  Created graph: {graph.Name} (ID: {graph.Id})");
        Console.WriteLine($"  Nodes: {string.Join(", ", graph.Nodes.Keys)}");
        Console.WriteLine($"  Edges: {graph.Edges.Count}\n");

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
