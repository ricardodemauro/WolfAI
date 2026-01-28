# WolfAI - Architecture Design

**Version**: 1.0  
**Last Updated**: January 28, 2026  
**Target Framework**: .NET 8 (LTS)

---

## 1. System Overview

WolfAI is a graph-based AI workflow engine built in C#. It orchestrates complex AI applications through a directed graph execution model where nodes represent units of work (LLM calls, tool invocations, custom logic) and edges define control flow with dynamic routing.

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     WolfAI Engine                           │
│                                                             │
│  ┌─────────────┐      ┌──────────────┐    ┌──────────────┐  │
│  │   Graph     │──────│   Execution  │────│  Checkpoint  │  │
│  │  Definition │      │    Engine    │    │   Manager    │  │
│  └─────────────┘      └──────┬───────┘    └──────────────┘  │
│                              │                              │
│                              │                              │
│  ┌───────────────────────────┼──────────────────────────┐   │
│  │         Node Pipeline     │                          │   │
│  │  ┌────────────┐  ┌────────▼───────┐  ┌───────────┐   │   │
│  │  │ Middleware │─▶│  Node Executor │─▶│  Routing │   │   │
│  │  └────────────┘  └────────────────┘  └───────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              Observability Layer                      │  │
│  │  ┌──────────────┐  ┌─────────────┐  ┌────────────┐    │  │
│  │  │ OpenTelemetry│  │   Logging   │  │  Metrics   │    │  │
│  │  └──────────────┘  └─────────────┘  └────────────┘    │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
           │                  │                  │
           ▼                  ▼                  ▼
    ┌───────────┐      ┌───────────┐     ┌──────────────┐
    │ LLM APIs  │      │   Tools   │     │  Data Stores │
    └───────────┘      └───────────┘     └──────────────┘
```

---

## 2. Core Components

### 2.1 Graph Components

#### 2.1.1 Graph

The container for the entire workflow definition.

**Class**: `Graph`

```csharp
public class Graph
{
    public string Id { get; init; }
    public string Name { get; init; }
    public IReadOnlyDictionary<string, Node> Nodes { get; init; }
    public IReadOnlyList<Edge> Edges { get; init; }
    public string EntryNodeId { get; init; }
}
```

**Responsibilities**:
- Container for nodes and edges
- Define entry point (always StartNode) and exit point (always EndNode)
- Validate graph structure (no orphaned nodes, StartNode exists, EndNode exists)
- Graphs must always start with StartNode and end with EndNode as entry/exit points

---

#### 2.1.2 Node

Abstract base class for all node types.

**Class**: `Node`

```csharp
public abstract class Node
{
    public string Id { get; init; }
    public string Name { get; init; }
    public abstract NodeType Type { get; }
    public List<INodeMiddleware> Middleware { get; init; }
    public Dictionary<string, object> Configuration { get; init; }
    
    public abstract Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken);
}

public enum NodeType
{
    Start,      // Special entry point
    End,        // Special exit point
    AI,         // LLM call node
    Tool        // Tool invocation node
}

public class NodeResult
{
    public bool Success { get; init; }
    public object Output { get; init; }
    public Dictionary<string, object> Variables { get; init; }
    public Exception Error { get; init; }
    public TimeSpan Duration { get; init; }
}
```

**Special Node Types** (Sealed):

**StartNode**
```csharp
public sealed class StartNode : Node
{
    public override NodeType Type => NodeType.Start;
    // No configuration needed - receives initial input
    // Always the entry point for graph execution
    
    public override Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Adds initial HumanMessage to context from external input
        // No-op execution, passes through to next edge
    }
}
```

**EndNode**
```csharp
public sealed class EndNode : Node
{
    public override NodeType Type => NodeType.End;
    // No configuration needed - receives final output
    // Always the exit point for graph execution
    
    public override Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Captures final output and terminates execution
        // No outgoing edges allowed
    }
}
```

**Concrete Node Types**:

**AINode**
```csharp
public class AINode : Node
{
    public override NodeType Type => NodeType.AI;
    
    // User-defined execution logic
    public Func<ExecutionContext, CancellationToken, Task<NodeResult>> ExecutionLogic { get; init; }
    
    public override Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Delegate to user-provided logic
        return ExecutionLogic(context, cancellationToken);
    }
}
```

**Notes on AINode**:
- Accepts any user-defined logic as a delegate
- Logic can call LLMs, perform C# operations, database queries, etc.
- All nodes share the same execution contract: `Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken)`
- Configuration is handled entirely by the user within their custom logic
- Middleware pipeline applies before and after execution

**ToolNode**
```csharp
public class ToolNode : Node
{
    public override NodeType Type => NodeType.Tool;
    public ITool Tool { get; init; }
    public ToolConfiguration ToolConfig { get; init; }
}

public class ToolConfiguration
{
    public string ToolName { get; init; }
    public Dictionary<string, string> ParameterMappings { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public RetryPolicy RetryPolicy { get; init; }
}
```

---

#### 2.1.3 Edge

Defines connections between nodes with routing logic.

**Class**: `Edge`

```csharp
public class Edge
{
    public string Id { get; init; }
    public string SourceNodeId { get; init; }
    public string TargetNodeId { get; init; }
    public Func<ExecutionContext, bool> RoutingFunction { get; init; }
    public int Priority { get; init; } = 0; // Lower = evaluated first
    public EdgeMetadata Metadata { get; init; }
}

public class EdgeMetadata
{
    public string Description { get; init; }
    public Dictionary<string, string> Tags { get; init; }
}
```

**Responsibilities**:
- Define source and target nodes
- Execute routing function to determine if edge should be activated
- Support priority-based evaluation when multiple edges exist
- Enable complex, context-aware routing logic

---

### 2.2 Message System

All messages inherit from a common base class and represent conversation history.

**Base Class**: `MessageBase`

```csharp
public abstract class MessageBase
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public abstract MessageType Type { get; }
    public Dictionary<string, object> Metadata { get; init; }
}

public enum MessageType
{
    Human,
    AI,
    ToolCall,
    Tool,
    System
}
```

**Concrete Message Types**:

**HumanMessage**
```csharp
public class HumanMessage : MessageBase
{
    public override MessageType Type => MessageType.Human;
    public string Content { get; init; }
}
```

**AIMessage**
```csharp
public class AIMessage : MessageBase
{
    public override MessageType Type => MessageType.AI;
    public string Content { get; init; }
    public string Model { get; init; }
    public TokenUsage TokenUsage { get; init; }
}
```

**ToolCallMessage**
```csharp
public class ToolCallMessage : MessageBase
{
    public override MessageType Type => MessageType.ToolCall;
    public string ToolName { get; init; }
    public Dictionary<string, object> Parameters { get; init; }
    public string CallId { get; init; }
}
```

**ToolMessage**
```csharp
public class ToolMessage : MessageBase
{
    public override MessageType Type => MessageType.Tool;
    public string ToolName { get; init; }
    public string CallId { get; init; } // References ToolCallMessage
    public object Result { get; init; }
    public bool Success { get; init; }
}
```

**SystemMessage**
```csharp
public class SystemMessage : MessageBase
{
    public override MessageType Type => MessageType.System;
    public string Content { get; init; }
}
```

**Token Usage Tracking**
```csharp
public class TokenUsage
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal EstimatedCost { get; init; }
}
```

---

### 2.3 Execution Context

Encapsulates runtime state during graph execution.

**Class**: `ExecutionContext`

```csharp
public class ExecutionContext
{
    public string ExecutionId { get; init; }  // Unique execution identifier
    public string ThreadId { get; init; }     // Conversation/thread identifier
    public string GraphId { get; init; }
    public string CurrentNodeId { get; set; }
    
    // State management
    public IDictionary<string, object> GlobalVariables { get; init; }  // Shared state across all nodes
    public VariableScope Variables { get; init; }  // Node-scoped variables
    public List<MessageBase> Messages { get; init; }
    public Stack<string> NodeExecutionHistory { get; init; }
    
    // Dependency Injection
    public IServiceProvider ServiceProvider { get; init; }  // Access to registered services
    
    // Observability
    public ILogger Logger { get; init; }
    public ActivitySource ActivitySource { get; init; }
    public Activity CurrentActivity { get; set; }
    
    // Control
    public CancellationToken CancellationToken { get; init; }
    public ExecutionMetrics Metrics { get; init; }
    
    // Metadata
    public DateTime StartedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}

public class VariableScope
{
    private readonly Dictionary<string, Dictionary<string, object>> _nodeVariables;
    private readonly IDictionary<string, object> _globalVariables;  // Reference to ExecutionContext.GlobalVariables
    
    public object GetGlobal(string key);
    public void SetGlobal(string key, object value);
    public object GetNodeVariable(string nodeId, string key);
    public void SetNodeVariable(string nodeId, string key, object value);
    public bool TryGetVariable(string key, out object value); // Tries node-level first, then global
}

public class ExecutionMetrics
{
    public int NodesExecuted { get; set; }
    public int TotalTokensUsed { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<NodeType, int> NodeTypeCounters { get; init; }
}
```

---

### 2.4 Checkpoint System

Enables time-travel debugging and state recovery.

**Class**: `Checkpoint`

```csharp
public class Checkpoint
{
    public string Id { get; init; }
    public string ExecutionId { get; init; }
    public string GraphId { get; init; }
    public DateTime CreatedAt { get; init; }
    
    // State snapshot
    public string CurrentNodeId { get; init; }
    public Dictionary<string, object> GlobalVariables { get; init; }
    public Dictionary<string, Dictionary<string, object>> NodeVariables { get; init; }
    public List<MessageBase> Messages { get; init; }
    public List<string> NodeExecutionHistory { get; init; }
    
    // Metadata
    public CheckpointTrigger Trigger { get; init; }
    public long SizeInBytes { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}

public enum CheckpointTrigger
{
    Auto,           // Automatic before each node
    Manual,         // User/code triggered
    Error,          // Created on error
    Conditional     // Based on condition
}
```

**Interface**: `ICheckpointStore`

```csharp
public interface ICheckpointStore
{
    Task SaveAsync(Checkpoint checkpoint, CancellationToken cancellationToken);
    Task<Checkpoint> LoadAsync(string checkpointId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Checkpoint>> ListAsync(string executionId, CancellationToken cancellationToken);
    Task DeleteAsync(string checkpointId, CancellationToken cancellationToken);
    Task<int> PruneAsync(CheckpointRetentionPolicy policy, CancellationToken cancellationToken);
}

public class CheckpointRetentionPolicy
{
    public int? MaxCheckpointsPerExecution { get; init; }
    public TimeSpan? MaxAge { get; init; }
    public long? MaxTotalSizeBytes { get; init; }
}
```

**Implementations**:

```csharp
public class InMemoryCheckpointStore : ICheckpointStore { }
public class FileSystemCheckpointStore : ICheckpointStore
{
    public string BasePath { get; init; }
}
public class SQLiteCheckpointStore : ICheckpointStore
{
    public string ConnectionString { get; init; }
}
```

---

### 2.5 Middleware System

Node-level middleware for intercepting and modifying execution.

**Interface**: `INodeMiddleware`

```csharp
public interface INodeMiddleware
{
    string Name { get; }
    int Priority { get; } // Lower executes first in before-phase
    
    Task OnBeforeExecuteAsync(
        NodeMiddlewareContext context,
        CancellationToken cancellationToken);
    
    Task OnAfterExecuteAsync(
        NodeMiddlewareContext context,
        NodeResult result,
        CancellationToken cancellationToken);
    
    Task OnErrorAsync(
        NodeMiddlewareContext context,
        Exception exception,
        CancellationToken cancellationToken);
}

public class NodeMiddlewareContext
{
    public Node Node { get; init; }
    public ExecutionContext ExecutionContext { get; init; }
    public List<MessageBase> Messages { get; init; }
    public Dictionary<string, object> Variables { get; init; }
}
```

**LLM-Specific Middleware Interface**:

```csharp
public interface ILLMNodeMiddleware : INodeMiddleware
{
    Task OnLLMRequestAsync(
        LLMRequest request,
        NodeMiddlewareContext context,
        CancellationToken cancellationToken);
    
    Task OnLLMResponseAsync(
        LLMResponse response,
        NodeMiddlewareContext context,
        CancellationToken cancellationToken);
}

public class LLMRequest
{
    public List<MessageBase> Messages { get; set; }
    public string SystemPrompt { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public class LLMResponse
{
    public string Content { get; set; }
    public TokenUsage TokenUsage { get; set; }
    public List<ToolCallMessage> ToolCalls { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

**Built-in Middleware Examples**:

```csharp
// Summarizes message history to reduce token count
public class MessageSummarizationMiddleware : ILLMNodeMiddleware { }

// Filters messages based on criteria
public class MessageFilteringMiddleware : INodeMiddleware { }

// Caches node outputs
public class CachingMiddleware : INodeMiddleware { }

// Implements retry logic
public class RetryMiddleware : INodeMiddleware { }

// Enforces token limits
public class TokenLimitMiddleware : ILLMNodeMiddleware { }
```

---

### 2.6 Execution Engine

Orchestrates graph execution with checkpoint management and observability.

**Class**: `GraphExecutionEngine`

```csharp
public class GraphExecutionEngine
{
    private readonly ICheckpointStore _checkpointStore;
    private readonly ILogger<GraphExecutionEngine> _logger;
    private readonly ActivitySource _activitySource;
    private readonly EngineConfiguration _configuration;
    
    public async Task<ExecutionResult> ExecuteAsync(
        Graph graph,
        ExecutionContext context,
        CancellationToken cancellationToken);
    
    public async Task<ExecutionContext> RestoreFromCheckpointAsync(
        string checkpointId,
        CancellationToken cancellationToken);
    
    public async Task<ExecutionResult> ReplayFromCheckpointAsync(
        string checkpointId,
        CancellationToken cancellationToken);
    
    private async Task<NodeResult> ExecuteNodeAsync(
        Node node,
        ExecutionContext context,
        CancellationToken cancellationToken);
    
    private async Task<Edge> DetermineNextEdgeAsync(
        Node currentNode,
        ExecutionContext context,
        CancellationToken cancellationToken);
}

public class ExecutionResult
{
    public bool Success { get; init; }
    public ExecutionContext Context { get; init; }
    public List<MessageBase> Messages { get; init; }
    public object FinalOutput { get; init; }
    public TimeSpan Duration { get; init; }
    public Exception Error { get; init; }
}

public class EngineConfiguration
{
    public int MaxConcurrentExecutions { get; init; } = 10;
    public TimeSpan DefaultNodeTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public CheckpointStrategy CheckpointStrategy { get; init; } = CheckpointStrategy.BeforeEachNode;
    public CheckpointRetentionPolicy CheckpointRetentionPolicy { get; init; }
    public bool EnableTelemetry { get; init; } = true;
    public TelemetryConfiguration TelemetryConfig { get; init; }
}

public enum CheckpointStrategy
{
    Never,
    BeforeEachNode,
    OnDemand,
    OnErrorOnly
}
```

---

### 2.7 LLM Provider Abstraction

Abstraction layer for multiple LLM providers.

**Interface**: `ILLMProvider`

```csharp
public interface ILLMProvider
{
    LLMProvider ProviderType { get; }
    
    Task<LLMResponse> CompletionAsync(
        LLMRequest request,
        CancellationToken cancellationToken);
    
    IAsyncEnumerable<LLMStreamChunk> StreamCompletionAsync(
        LLMRequest request,
        CancellationToken cancellationToken);
}

public enum LLMProvider
{
    OpenAI,
    Azure,
    Anthropic,
    Ollama,
    Custom
}

public class LLMStreamChunk
{
    public string Delta { get; init; }
    public bool IsFirstToken { get; init; }
    public bool IsComplete { get; init; }
    public TokenUsage TokenUsage { get; init; }
}
```

**Concrete Implementations**:

```csharp
public class OpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
}

public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
}

public class AzureOpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
}

public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
}
```

---

### 2.8 Tool System

Abstraction for tool/function invocations.

**Interface**: `ITool`

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolParameterSchema ParameterSchema { get; }
    
    Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        ExecutionContext context,
        CancellationToken cancellationToken);
}

public class ToolParameterSchema
{
    public List<ToolParameter> Parameters { get; init; }
    public List<string> RequiredParameters { get; init; }
}

public class ToolParameter
{
    public string Name { get; init; }
    public string Type { get; init; } // string, number, boolean, object, array
    public string Description { get; init; }
    public object DefaultValue { get; init; }
    public Dictionary<string, object> Constraints { get; init; } // min, max, pattern, etc.
}

public class ToolResult
{
    public bool Success { get; init; }
    public object Data { get; init; }
    public string Error { get; init; }
    public TimeSpan Duration { get; init; }
}
```

**Tool Definition for LLM**:

```csharp
public class ToolDefinition
{
    public string Name { get; init; }
    public string Description { get; init; }
    public Dictionary<string, object> Parameters { get; init; } // JSON Schema format
    
    public static ToolDefinition FromTool(ITool tool)
    {
        // Converts ITool to format expected by LLM APIs
    }
}
```

**Built-in Tools**:

```csharp
public class WebSearchTool : ITool { }
public class CalculatorTool : ITool { }
public class HttpRequestTool : ITool { }
```

---

### 2.9 Graph Builder (Fluent API)

Provides a fluent API for constructing graphs programmatically.

**Class**: `GraphBuilder`

```csharp
public class GraphBuilder
{
    private readonly string _graphId;
    private readonly string _name;
    private readonly Dictionary<string, Node> _nodes;
    private readonly List<Edge> _edges;
    
    public GraphBuilder(string name);
    
    public GraphBuilder AddAINode(
        string id,
        string name,
        Func<ExecutionContext, CancellationToken, Task<NodeResult>> executionLogic,
        Action<AINodeBuilder> configure = null);
    
    public GraphBuilder AddToolNode(
        string id,
        string name,
        ITool tool,
        Action<ToolNodeBuilder> configure = null);
    
    public GraphBuilder AddEdge(
        string id,
        string sourceNodeId,
        string targetNodeId,
        Func<ExecutionContext, bool> routingFunction,
        int priority = 0);
    
    public Graph Build();
}

public class AINodeBuilder
{
    public AINodeBuilder AddMiddleware(INodeMiddleware middleware);
}

public class ToolNodeBuilder
{
    public ToolNodeBuilder WithTimeout(TimeSpan timeout);
    public ToolNodeBuilder WithRetryPolicy(RetryPolicy policy);
    public ToolNodeBuilder WithParameterMapping(string paramName, string expression);
    public ToolNodeBuilder AddMiddleware(INodeMiddleware middleware);
}
```

### 2.10 AINode Usage Examples

**LLM-based AINode**:
```csharp
var aiLogic = async (ExecutionContext context, CancellationToken ct) =>
{
    var llmProvider = new OpenAIProvider(apiKey);
    var request = new LLMRequest
    {
        Messages = context.Messages,
        SystemPrompt = "You are a helpful assistant",
        Parameters = new() { ["temperature"] = 0.7 }
    };
    
    var response = await llmProvider.CompletionAsync(request, ct);
    
    context.Messages.Add(new AIMessage { Content = response.Content });
    
    return new NodeResult
    {
        Success = true,
        Output = response.Content
    };
};

// Use in graph builder
var graph = new GraphBuilder("MyWorkflow")
    .AddAINode("classifier", "AI Classifier", aiLogic)
    .Build();
```

**Custom C# logic AINode**:
```csharp
var customLogic = async (ExecutionContext context, CancellationToken ct) =>
{
    // Access global variables
    var userId = (string)context.GlobalVariables["userId"];
    
    // Perform database or business logic
    var data = await LoadUserDataAsync(userId, ct);
    
    // Update message history
    context.Messages.Add(new SystemMessage { Content = $"Loaded data: {data}" });
    
    return new NodeResult
    {
        Success = true,
        Output = data,
        Variables = new() { ["processedData"] = data }
    };
};

graph.AddAINode("dataLoader", "Load User Data", customLogic, config => 
    config.AddMiddleware(new CachingMiddleware()));
```

---

## 3. Libraries and Dependencies

### 3.1 Core Dependencies

**Target Framework**:
- `.NET 8` (LTS)

**Microsoft Packages**:
- `Microsoft.Extensions.DependencyInjection` - Dependency injection container
- `Microsoft.Extensions.Logging.Abstractions` - Logging abstractions
- `Microsoft.Extensions.Configuration` - Configuration management
- `Microsoft.Extensions.Options` - Options pattern
- `System.Text.Json` - JSON serialization/deserialization

**OpenTelemetry**:
- `OpenTelemetry` - Core library
- `OpenTelemetry.Api` - API for instrumentation
- `OpenTelemetry.Exporter.Console` - Console exporter for development
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP exporter
- `OpenTelemetry.Exporter.Jaeger` - Jaeger exporter
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` - Prometheus metrics

**HTTP & Networking**:
- `System.Net.Http` - Built-in HTTP client
- `Microsoft.Extensions.Http` - HttpClient factory
- `Microsoft.Extensions.Http.Resilience` - Polly integration for resilience

### 3.2 LLM Provider SDKs

Direct HTTP client implementations for each provider:
- OpenAI API - Custom HTTP client with OpenAI API spec
- Anthropic API - Custom HTTP client with Claude API spec
- Azure OpenAI API - Custom HTTP client with Azure endpoints
- Ollama API - Custom HTTP client for local models

No heavy SDK dependencies - direct HTTP/JSON for maximum control and minimal overhead.

### 3.3 Storage Dependencies

**In-Memory**: No additional dependencies

**File System**: 
- `System.IO` - Built-in file I/O

**SQLite**:
- `Microsoft.Data.Sqlite` - SQLite provider for .NET
- `Microsoft.EntityFrameworkCore.Sqlite` - (Optional) EF Core for easier data access

### 3.4 Testing Dependencies

- `xUnit` - Test framework
- `Moq` - Mocking library
- `FluentAssertions` - Assertion library
- `Microsoft.Extensions.Logging.Testing` - Testing utilities for logging

### 3.5 Build and Tooling

- `Microsoft.CodeAnalysis.NetAnalyzers` - Static code analysis
- `StyleCop.Analyzers` - Code style enforcement
- `coverlet.collector` - Code coverage

---

## 4. Component Responsibilities

### 4.1 Graph Layer

**Graph**
- Define workflow structure
- Validate graph integrity (no orphaned nodes, cycles if not allowed)
- Store global state template
- Provide entry point

**Node**
- Execute specific task (LLM call, tool invocation, logic)
- Manage node-level middleware chain
- Produce structured output
- Report metrics and errors

**Edge**
- Connect nodes in directed flow
- Evaluate routing conditions dynamically
- Support priority-based evaluation
- Enable conditional branching

### 4.2 Execution Layer

**GraphExecutionEngine**
- Orchestrate overall execution flow
- Manage execution lifecycle
- Create and restore checkpoints
- Handle errors and cancellation
- Emit telemetry and metrics

**ExecutionContext**
- Maintain runtime state (variables, messages, history)
- Provide scoped variable access (global vs node-level)
- Track performance metrics
- Enable cross-cutting concerns (logging, tracing)

**VariableScope**
- Manage hierarchical variable storage (global + node-level)
- Support variable resolution with fallback
- Enable variable isolation per node

### 4.3 Message Layer

**MessageBase** (and subclasses)
- Represent conversation history
- Capture timestamps and metadata
- Support different interaction types (human, AI, tool)
- Enable message-based routing decisions

### 4.4 Checkpoint Layer

**Checkpoint**
- Snapshot execution state at point in time
- Enable rollback to previous state
- Support replay from checkpoint
- Facilitate debugging and testing

**ICheckpointStore** (and implementations)
- Persist checkpoints to storage
- Retrieve checkpoints by ID or execution ID
- Manage checkpoint lifecycle (creation, retrieval, deletion)
- Enforce retention policies (pruning)

### 4.5 Middleware Layer

**INodeMiddleware**
- Intercept node execution
- Modify request/response data
- Transform messages and context
- Apply cross-cutting concerns (caching, retry, filtering)

**ILLMNodeMiddleware**
- Specialize middleware for LLM nodes
- Modify prompts before API calls
- Transform LLM responses
- Manage token limits

### 4.6 LLM Provider Layer

**ILLMProvider** (and implementations)
- Abstract LLM API differences
- Handle HTTP communication
- Parse provider-specific responses
- Support streaming
- Track token usage and costs

### 4.7 Tool Layer

**ITool**
- Define executable function/tool
- Provide parameter schema
- Execute with parameters
- Return structured results

**ToolDefinition**
- Convert ITool to LLM-compatible format (JSON schema)
- Enable LLM to understand available tools
- Support function calling

### 4.8 Observability Layer

**OpenTelemetry Integration**
- Create distributed traces (spans)
- Record metrics (counters, histograms, gauges)
- Add semantic attributes
- Export to backends (Jaeger, Prometheus, OTLP)

**Logging**
- Structured logging with context
- Correlation IDs for tracing
- Performance metrics
- Error tracking

---

## 5. Key Design Patterns

### 5.1 Strategy Pattern
- Node implementations (AINode, ToolNode)
- LLM provider implementations
- Checkpoint storage implementations

### 5.2 Chain of Responsibility
- Middleware pipeline execution
- Edge routing evaluation

### 5.3 Builder Pattern
- GraphBuilder for fluent graph construction
- Node-specific builders (AINodeBuilder, ToolNodeBuilder)

### 5.4 Template Method
- Node base class with ExecuteAsync template
- Middleware hook points (before, after, error)

### 5.5 Repository Pattern
- ICheckpointStore abstraction
- Storage implementation independence

### 5.6 Factory Pattern
- Node creation from configuration
- LLM provider instantiation

---

## 6. Threading and Async Model

### 6.1 Async/Await Throughout
- All I/O operations are async (LLM calls, tool execution, storage)
- Cancellation token support for graceful shutdown
- No synchronous blocking operations

### 6.2 Concurrency Control
- Execution engine manages concurrent executions (configurable limit)
- Thread-safe variable access within execution context
- Checkpoint creation is atomic per execution

### 6.3 Streaming Support
- IAsyncEnumerable for LLM streaming responses
- Event-driven token-by-token processing
- Backpressure handling

---

## 7. Error Handling Strategy

### 7.1 Error Types

```csharp
public class WolfException : Exception
{
    public string Code { get; init; }
}

public class GraphValidationException : WolfException { }
public class NodeExecutionException : WolfException
{
    public string NodeId { get; init; }
}
public class LLMProviderException : WolfException
{
    public LLMProvider Provider { get; init; }
}
public class TimeoutException : WolfException { }
public class CheckpointException : WolfException { }
```

### 7.2 Error Recovery

**Retry Policies**:
```csharp
public class RetryPolicy
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; init; } = 2.0;
    public Func<Exception, bool> ShouldRetry { get; init; }
}
```

**Circuit Breaker** (via Polly):
- Prevent cascading failures to external services
- Configurable per LLM provider or tool

**Graceful Degradation**:
- Continue execution with partial results when possible
- Fallback edges for error scenarios

---

## 8. Configuration Model

### 8.1 Engine Configuration

```csharp
public class WolfEngineOptions
{
    public EngineConfiguration Engine { get; init; }
    public TelemetryConfiguration Telemetry { get; init; }
    public LoggingConfiguration Logging { get; init; }
    public Dictionary<string, LLMProviderConfiguration> LLMProviders { get; init; }
}

public class TelemetryConfiguration
{
    public bool Enabled { get; init; } = true;
    public string ServiceName { get; init; } = "WolfAI";
    public List<ExporterConfiguration> Exporters { get; init; }
    public double SamplingRate { get; init; } = 1.0; // 100%
}

public class ExporterConfiguration
{
    public ExporterType Type { get; init; } // Console, OTLP, Jaeger, Prometheus
    public Dictionary<string, string> Settings { get; init; }
}

public class LLMProviderConfiguration
{
    public LLMProvider Provider { get; init; }
    public string ApiKey { get; init; }
    public string Endpoint { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
    public RetryPolicy RetryPolicy { get; init; }
}
```

### 8.2 Configuration Sources

- appsettings.json
- Environment variables
- Code-based (Options pattern)
- Azure Key Vault / Secrets management

---

## 9. Serialization Strategy

### 9.1 System.Text.Json

**Configuration**:
```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false, // Compact for storage
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = {
        new JsonStringEnumConverter(),
        new MessageBaseConverter(), // Custom polymorphic converter
        new NodeConverter()
    }
};
```

**Polymorphic Serialization**:
- Custom converters for MessageBase hierarchy
- Type discriminators for deserialization
- Support for derived types (AIMessage, HumanMessage, etc.)

### 9.2 Checkpoint Serialization

Checkpoints are serialized to JSON for all storage backends:
- In-Memory: Direct object storage (no serialization)
- File System: JSON files on disk
- SQLite: JSON stored in TEXT column

---

## 10. Dependency Injection Setup

### 10.1 Service Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWolfAI(
        this IServiceCollection services,
        Action<WolfEngineOptions> configure)
    {
        // Core services
        services.AddSingleton<GraphExecutionEngine>();
        services.AddSingleton<ActivitySource>(sp => 
            new ActivitySource("WolfAI", "1.0.0"));
        
        // Checkpoint store
        services.AddSingleton<ICheckpointStore, InMemoryCheckpointStore>();
        
        // LLM Providers
        services.AddHttpClient<ILLMProvider, OpenAIProvider>();
        services.AddHttpClient<ILLMProvider, AnthropicProvider>();
        
        // Tools
        services.AddTransient<ITool, WebSearchTool>();
        services.AddTransient<ITool, CalculatorTool>();
        
        // Middleware
        services.AddTransient<INodeMiddleware, RetryMiddleware>();
        services.AddTransient<ILLMNodeMiddleware, MessageSummarizationMiddleware>();
        
        // Configuration
        services.Configure<WolfEngineOptions>(configure);
        
        // OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddSource("WolfAI")
                .AddOtlpExporter())
            .WithMetrics(builder => builder
                .AddMeter("WolfAI")
                .AddPrometheusExporter());
        
        return services;
    }
}
```

---

## 11. Project Structure

```
WolfAI/
├── src/
│   ├── WolfAI.Core/                    # Core abstractions and models
│   │   ├── Graph/
│   │   │   ├── Graph.cs
│   │   │   ├── Node.cs
│   │   │   ├── Edge.cs
│   │   │   └── GraphBuilder.cs
│   │   ├── Messages/
│   │   │   ├── MessageBase.cs
│   │   │   ├── HumanMessage.cs
│   │   │   ├── AIMessage.cs
│   │   │   ├── ToolCallMessage.cs
│   │   │   └── ToolMessage.cs
│   │   ├── Execution/
│   │   │   ├── ExecutionContext.cs
│   │   │   ├── ExecutionResult.cs
│   │   │   └── VariableScope.cs
│   │   ├── Checkpoints/
│   │   │   ├── Checkpoint.cs
│   │   │   └── ICheckpointStore.cs
│   │   ├── Middleware/
│   │   │   ├── INodeMiddleware.cs
│   │   │   └── ILLMNodeMiddleware.cs
│   │   ├── Tools/
│   │   │   ├── ITool.cs
│   │   │   └── ToolDefinition.cs
│   │   └── LLM/
│   │       ├── ILLMProvider.cs
│   │       ├── LLMRequest.cs
│   │       └── LLMResponse.cs
│   │
│   ├── WolfAI.Engine/                  # Execution engine implementation
│   │   ├── GraphExecutionEngine.cs
│   │   ├── NodeExecutor.cs
│   │   └── EdgeRouter.cs
│   │
│   ├── WolfAI.Nodes/                   # Concrete node implementations
│   │   ├── StartNode.cs
│   │   ├── EndNode.cs
│   │   ├── AINode.cs
│   │   └── ToolNode.cs
│   │
│   ├── WolfAI.LLM/                     # LLM provider implementations
│   │   ├── OpenAI/
│   │   │   └── OpenAIProvider.cs
│   │   ├── Anthropic/
│   │   │   └── AnthropicProvider.cs
│   │   ├── Azure/
│   │   │   └── AzureOpenAIProvider.cs
│   │   └── Ollama/
│   │       └── OllamaProvider.cs
│   │
│   ├── WolfAI.Checkpoints/             # Checkpoint storage implementations
│   │   ├── InMemoryCheckpointStore.cs
│   │   ├── FileSystemCheckpointStore.cs
│   │   └── SQLiteCheckpointStore.cs
│   │
│   ├── WolfAI.Middleware/              # Built-in middleware
│   │   ├── MessageSummarizationMiddleware.cs
│   │   ├── MessageFilteringMiddleware.cs
│   │   ├── CachingMiddleware.cs
│   │   ├── RetryMiddleware.cs
│   │   └── TokenLimitMiddleware.cs
│   │
│   ├── WolfAI.Tools/                   # Built-in tools
│   │   ├── WebSearchTool.cs
│   │   ├── CalculatorTool.cs
│   │   └── HttpRequestTool.cs
│   │
│   └── WolfAI.Telemetry/               # OpenTelemetry integration
│       ├── WolfActivitySource.cs
│       ├── WolfMetrics.cs
│       └── TelemetryExtensions.cs
│
├── tests/
│   ├── WolfAI.Core.Tests/
│   ├── WolfAI.Engine.Tests/
│   ├── WolfAI.Integration.Tests/
│   └── WolfAI.Performance.Tests/
│
├── samples/
│   ├── SimpleWorkflow/
│   ├── ToolInvocation/
│   └── MultiAgentSystem/
│
└── .docs/
    ├── 01-specs.md
    ├── 02-architecture.md
    └── 03-api-reference.md
```

---

## 12. Implementation Phases

### Phase 1: Foundation (MVP)
**Goal**: Basic graph execution with AI node support

- [ ] Core graph model (Graph, Node, Edge)
- [ ] GraphBuilder fluent API
- [ ] ExecutionContext and VariableScope
- [ ] GraphExecutionEngine basic flow
- [ ] StartNode and EndNode special nodes
- [ ] AINode with OpenAI provider
- [ ] Message types (HumanMessage, AIMessage)
- [ ] In-memory checkpoint store
- [ ] Basic logging

**Deliverable**: Can execute simple linear AI workflows with proper entry/exit points

### Phase 2: Tool Support
**Goal**: Enable AI tool calling workflows

- [ ] ToolNode implementation
- [ ] ITool interface and built-in tools
- [ ] ToolCallMessage and ToolMessage
- [ ] Tool registration and discovery
- [ ] LLM provider tool support
- [ ] Edge routing function evaluation
- [ ] Priority-based edge selection

**Deliverable**: Can execute tool-calling workflows with dynamic routing

### Phase 3: Middleware System
**Goal**: Enable cross-cutting concerns

- [ ] Middleware interface and pipeline
- [ ] Message summarization middleware
- [ ] Token limiting middleware
- [ ] Caching middleware
- [ ] Retry middleware

**Deliverable**: Production-ready middleware support

### Phase 4: Observability
**Goal**: Full OpenTelemetry integration

- [ ] Distributed tracing with spans
- [ ] Metrics (counters, histograms, gauges)
- [ ] Exporter configuration
- [ ] Structured logging improvements
- [ ] Cost tracking

**Deliverable**: Production-grade observability

### Phase 5: Persistence & Advanced Features
**Goal**: Persistent checkpoints and additional providers

- [ ] File system checkpoint store
- [ ] SQLite checkpoint store
- [ ] Checkpoint retention policies
- [ ] Replay from checkpoint
- [ ] Additional LLM providers (Anthropic, Azure, Ollama)

**Deliverable**: Production-ready for persistent workflows

---

## 13. Open Questions and Future Considerations

### 13.1 Parallelization
- Should we support parallel node execution within a graph?
- How to handle fan-out/fan-in patterns?
- Synchronization primitives for parallel execution?

### 13.2 Security
- How to secure API keys and secrets?
- Input validation and sanitization for LLM prompts
- Rate limiting and quota management
- Audit logging for compliance

### 13.3 Schema Evolution
- How to handle graph schema changes over time?
- Migration strategy for checkpoints with old schema
- Versioning strategy for graphs

### 13.4 Testing Strategy
- Unit testing for nodes (mocking LLM responses)
- Integration testing for full workflows
- Performance testing and benchmarks
- How to test non-deterministic LLM behavior?

### 13.5 Deployment
- Should we provide a hosted service?
- Container/Docker support
- Kubernetes deployment patterns
- Horizontal scaling considerations

---

**Status**: Draft for Review  
**Next Steps**: Review architecture, address open questions, proceed with Phase 1 implementation
