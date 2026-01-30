# Immutable ExecutionContext Pattern

**Date**: January 30, 2026  
**Status**: Implemented in code, architecture updated

---

## Overview

WolfAI uses an **immutable ExecutionContext pattern** where nodes cannot directly mutate execution state. Instead, nodes return state changes via `NodeResult`, and the `GraphExecutionEngine` creates new ExecutionContext instances with merged state.

This design enables:
- ✅ **True time-travel debugging** - Previous states are never mutated
- ✅ **Thread-safety** - No shared mutable state between concurrent executions
- ✅ **Predictable testing** - Nodes are pure functions (context in → result out)
- ✅ **Clear separation of concerns** - Nodes compute, engine orchestrates
- ✅ **Easier reasoning** - State transitions are explicit

---

## Core Principles

### 1. ExecutionContext Is Read-Only for Nodes

```csharp
public interface IExecutionContext
{
    // All collections are READ-ONLY
    IReadOnlyDictionary<string, object?> GlobalVariables { get; }
    IReadOnlyList<BaseMessage> Messages { get; }
    IReadOnlyList<string> NodeExecutionHistory { get; }
    IReadOnlyDictionary<string, object?> Metadata { get; }
    
    // NO mutation methods
    // void AddMessage(...) ❌ REMOVED
    // void RecordNodeExecution(...) ❌ REMOVED
}
```

### 2. NodeResult Returns State Changes

```csharp
public class NodeResult
{
    public bool Success { get; init; }
    public object? Output { get; init; }
    
    // NEW: Messages to append (not mutate context)
    public IReadOnlyList<BaseMessage> Messages { get; init; }
    
    // NEW: Variables to merge (not mutate context)
    public IReadOnlyDictionary<string, object?> Variables { get; init; }
    
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
}
```

### 3. Graph Creates New Contexts

Only `GraphExecutionEngine` can create new ExecutionContext instances:

```csharp
// After node execution, create NEW context with merged state
var updatedContext = currentContext.WithUpdates(
    newMessages: nodeResult.Messages,
    newVariables: nodeResult.Variables,
    recordNodeExecution: currentNodeId
);

// Original context is unchanged (immutable)
// Continue execution with updatedContext
```

---

## Execution Flow

### Before (Mutable - WRONG):

```csharp
// ❌ OLD WAY - Nodes mutate context directly
var result = await node.ExecuteAsync(context, ct);

// Inside node:
context.AddMessage(new AIMessage(...));  // BAD: Mutates shared state
context.Variables.SetGlobal("key", value);  // BAD: Side effects

// Problem: Context is mutated, can't rollback, not thread-safe
```

### After (Immutable - CORRECT):

```csharp
// ✅ NEW WAY - Nodes return state changes
var result = await node.ExecuteAsync(context, ct);

// Inside node:
var messages = new List<BaseMessage> { new AIMessage(...) };
var variables = new Dictionary<string, object?> { ["key"] = value };

return new NodeResult
{
    Success = true,
    Output = response,
    Messages = messages,  // To be appended
    Variables = variables,  // To be merged
    Duration = elapsed
};

// Engine merges state into NEW context
context = context.WithUpdates(
    newMessages: result.Messages,
    newVariables: result.Variables,
    recordNodeExecution: node.Id
);

// Benefits: Original context unchanged, can checkpoint, thread-safe
```

---

## Node Implementation Pattern

### AINode Example (User-Defined Logic):

```csharp
var aiNode = new AINode
{
    Id = "llm-call",
    Name = "LLM Classifier",
    ExecutionLogic = async (context, ct) =>
    {
        // 1. READ from context (read-only)
        var userInput = context.GlobalVariables["input"];
        var previousMessages = context.Messages;
        
        // 2. Execute logic (LLM call, computation, etc.)
        var llmProvider = new OpenAIProvider();
        var response = await llmProvider.CompletionAsync(new LLMRequest
        {
            Messages = previousMessages.ToList(),
            SystemPrompt = "You are a classifier"
        }, ct);
        
        // 3. RETURN state changes (don't mutate context!)
        var newMessages = new List<BaseMessage>
        {
            new AIMessage(
                id: Guid.NewGuid().ToString(),
                content: new MessageContent { SimpleContent = response.Content },
                usageMetadata: response.TokenUsage
            )
        };
        
        var newVariables = new Dictionary<string, object?>
        {
            ["classification"] = response.Content,
            ["tokensUsed"] = response.TokenUsage.TotalTokens
        };
        
        return NodeResult.SuccessResult(
            output: response.Content,
            messages: newMessages,
            variables: newVariables,
            duration: TimeSpan.FromSeconds(2.5)
        );
    }
};
```

### StartNode Example:

```csharp
public sealed class StartNode : Node
{
    public override async Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Extract initial input from global variables
        var input = context.GlobalVariables.TryGetValue("input", out var value)
            ? value?.ToString()
            : null;
        
        if (string.IsNullOrEmpty(input))
        {
            return NodeResult.FailureResult("No input provided");
        }
        
        // CREATE initial HumanMessage (don't add to context!)
        var messages = new List<BaseMessage>
        {
            new HumanMessage(
                id: Guid.NewGuid().ToString(),
                content: new MessageContent { SimpleContent = input }
            )
        };
        
        return NodeResult.SuccessResult(
            output: input,
            messages: messages,  // Engine will append to context
            variables: null,  // No variables to add
            duration: TimeSpan.Zero
        );
    }
}
```

### EndNode Example:

```csharp
public sealed class EndNode : Node
{
    public override async Task<NodeResult> ExecuteAsync(
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        // READ final state (no mutations)
        var lastMessage = context.Messages.LastOrDefault();
        var finalOutput = lastMessage?.Content.SimpleContent ?? "No output";
        
        // Return result (no new messages or variables)
        return NodeResult.SuccessResult(
            output: finalOutput,
            messages: Array.Empty<BaseMessage>(),  // No new messages
            variables: null,  // No new variables
            duration: TimeSpan.Zero
        );
    }
}
```

---

## GraphExecutionEngine Responsibilities

The engine is the **only** component that creates new ExecutionContext instances:

```csharp
public async Task<ExecutionResult> ExecuteAsync(
    Graph graph,
    ExecutionContext initialContext,
    CancellationToken cancellationToken)
{
    var context = initialContext;
    var currentNode = graph.EntryNode;
    
    while (currentNode != null)
    {
        // 1. Execute node (pure function - returns result)
        var nodeResult = await NodeExecutor.ExecuteAsync(
            currentNode, 
            context,  // Read-only
            cancellationToken
        );
        
        if (!nodeResult.Success)
        {
            return ExecutionResult.Failure(nodeResult.Error, context);
        }
        
        // 2. Create NEW context with merged state
        context = context.WithUpdates(
            newMessages: nodeResult.Messages,
            newVariables: nodeResult.Variables,
            newNodeId: currentNode.Id,
            recordNodeExecution: currentNode.Id
        );
        
        // 3. Optional: Checkpoint the NEW context
        if (shouldCheckpoint)
        {
            await checkpointStore.SaveAsync(
                new Checkpoint
                {
                    ExecutionContext = context.CreateSnapshot()
                },
                cancellationToken
            );
        }
        
        // 4. Determine next node via routing
        var nextEdge = await EdgeRouter.DetermineNextEdgeAsync(
            currentNode, 
            context,  // NEW context with updated state
            cancellationToken
        );
        
        currentNode = nextEdge != null 
            ? graph.Nodes[nextEdge.TargetNodeId] 
            : null;  // Terminal node
    }
    
    return ExecutionResult.Success(context);
}
```

---

## Checkpoint Restoration

Restoring from a checkpoint creates a **new** ExecutionContext:

```csharp
// Load checkpoint
var checkpoint = await checkpointStore.LoadAsync(checkpointId, ct);

// Create NEW context from snapshot (factory method)
var restoredContext = ExecutionContext.FromSnapshot(
    snapshot: checkpoint.ExecutionContext,
    serviceProvider: serviceProvider,
    logger: logger,
    activitySource: activitySource,
    cancellationToken: ct
);

// Continue execution with restored context
var result = await engine.ExecuteAsync(graph, restoredContext, ct);
```

---

## Benefits Realized

### 1. Time-Travel Debugging

```csharp
// Checkpoint before risky node
var checkpoint1 = await SaveCheckpoint(context);

// Execute node (creates NEW context)
var result = await node.ExecuteAsync(context, ct);
context = context.WithUpdates(...);

// Something went wrong? Restore from checkpoint1
var restoredContext = ExecutionContext.FromSnapshot(checkpoint1);
// Original checkpoint1 context is UNCHANGED - can retry infinitely
```

### 2. Thread-Safety

```csharp
// Execute multiple graphs concurrently with same initial context
var initialContext = new ExecutionContext(...);

await Task.WhenAll(
    engine.ExecuteAsync(graph1, initialContext, ct),
    engine.ExecuteAsync(graph2, initialContext, ct),
    engine.ExecuteAsync(graph3, initialContext, ct)
);

// Safe! Each execution creates its own context chain
// No shared mutable state
```

### 3. Pure Function Testing

```csharp
[Fact]
public async Task Node_Should_Return_Messages_Not_Mutate_Context()
{
    // Arrange
    var context = CreateTestContext();
    var initialMessageCount = context.Messages.Count;
    
    // Act
    var result = await node.ExecuteAsync(context, CancellationToken.None);
    
    // Assert
    context.Messages.Count.Should().Be(initialMessageCount);  // Unchanged!
    result.Messages.Should().HaveCount(1);  // New messages returned
    result.Variables.Should().ContainKey("output");  // Variables returned
}
```

### 4. Explicit State Transitions

```csharp
// Clear flow: context1 → node → result → context2
var context1 = initialContext;
var result = await node.ExecuteAsync(context1, ct);
var context2 = context1.WithUpdates(result.Messages, result.Variables);

// Easy to reason about:
// - context1 is still valid (can rollback)
// - result shows what changed
// - context2 is new state (can checkpoint)
```

---

## Migration Guide (For Future Phases)

### Middleware (Phase 3)

Middleware will also return state changes, not mutate:

```csharp
public interface INodeMiddleware
{
    // Before execution: Can modify what gets passed to node
    Task<(IExecutionContext, NodeResult?)> OnBeforeExecuteAsync(
        IExecutionContext context,
        CancellationToken ct
    );
    
    // After execution: Can modify result before engine merges
    Task<NodeResult> OnAfterExecuteAsync(
        IExecutionContext context,
        NodeResult result,
        CancellationToken ct
    );
}
```

### ToolNode (Phase 2)

ToolNode follows same pattern:

```csharp
public override async Task<NodeResult> ExecuteAsync(
    IExecutionContext context,
    CancellationToken ct)
{
    // Extract ToolCallMessage from last AI message
    var lastMessage = context.Messages.OfType<AIMessage>().LastOrDefault();
    var toolCall = lastMessage?.ToolCalls.FirstOrDefault();
    
    // Execute tool
    var tool = Tools[toolCall.Name];
    var toolResult = await tool.ExecuteAsync(toolCall.Args, context, ct);
    
    // Return ToolMessage (don't mutate context)
    var messages = new List<BaseMessage>
    {
        new ToolMessage(
            id: Guid.NewGuid().ToString(),
            content: new MessageContent { SimpleContent = toolResult.Data },
            toolCallId: toolCall.Id,
            status: "success"
        )
    };
    
    return NodeResult.SuccessResult(
        output: toolResult.Data,
        messages: messages
    );
}
```

---

## Summary

| Aspect | Old (Mutable) | New (Immutable) |
|--------|---------------|-----------------|
| Context mutation | ✅ Allowed | ❌ Not allowed |
| State updates | `context.AddMessage()` | `context.WithUpdates(...)` |
| Node responsibility | Compute + Mutate | Compute only |
| Engine responsibility | Orchestrate only | Orchestrate + Merge state |
| Thread-safety | ❌ Requires locks | ✅ Inherently safe |
| Time-travel | ⚠️ Difficult | ✅ Trivial |
| Testing | ⚠️ Side effects | ✅ Pure functions |
| Rollback | ❌ Impossible | ✅ Easy |

---

**Conclusion**: The immutable ExecutionContext pattern is the foundation of WolfAI's reliability, debuggability, and scalability. All nodes **must** follow the pattern: read from context, return state changes via NodeResult, let the engine merge.

