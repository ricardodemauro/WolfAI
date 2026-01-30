# WolfAI - MVP Implementation Tasks

**Target**: Phase 1 - Foundation (MVP)  
**Goal**: Basic graph execution with AI node support  
**Target Framework**: .NET 8 (LTS)

---

## Overview

This document breaks down the MVP implementation into concrete, actionable tasks for a senior C# developer. Each task is scoped, includes dependencies, and references the architecture and specification documents.

**MVP Deliverable**: A working graph-based AI workflow engine that can execute simple linear AI workflows with proper entry/exit points and basic LLM integration.

---

## Architecture Reference

- **Core Components**: See [Architecture 2.1](02-architecture.md#21-graph-components) - Graph Components
- **Message System**: See [Architecture 2.2](02-architecture.md#22-message-system) - Message System
- **Execution Context**: See [Architecture 2.3](02-architecture.md#23-execution-context)
- **Checkpoint System**: See [Architecture 2.4](02-architecture.md#24-checkpoint-system)
- **Execution Engine**: See [Architecture 2.6](02-architecture.md#26-execution-engine)
- **LLM Provider Abstraction**: See [Architecture 2.7](02-architecture.md#27-llm-provider-abstraction)

---

## Task Breakdown

### **Phase 1.1: Core Domain Models** 
*Dependencies: None*  
*Estimated Effort: 2-3 days*

#### Task 1.1.1: Implement Graph, Node, and Edge Base Classes ✅ **COMPLETED**

**Objective**: Create the foundational graph model classes.

**Acceptance Criteria**:
- [x] `Graph` class with properties: `Id`, `Name`, `Nodes` (IReadOnlyDictionary), `Edges` (IReadOnlyList), `EntryNodeId`
- [x] `Node` abstract base class with: `Id`, `Name`, `NodeType` property, `Middleware` collection, and `ExecuteAsync` abstract method
- [x] `NodeType` enum: `Start`, `End`, `AI`, `Tool`
- [ ] `NodeResult` class with: `Success`, `Output`, `Messages` (IReadOnlyList<BaseMessage> to append), `Variables` (IReadOnlyDictionary<string, object> to merge), `Error`, `Duration`
- [x] `Edge` class with: `Id`, `SourceNodeId`, `TargetNodeId`, `RoutingFunction` (Func<ExecutionContext, bool>), `Priority`
- [x] `EdgeMetadata` class with: `Description`, `Tags`

**Reference**: [Architecture 2.1.1-2.1.3](02-architecture.md#211-graph)

**Implementation Notes**:
- Use record types for value object semantics where appropriate
- Make classes immutable using `init` properties
- Ensure threading safety for concurrent access

**Tests to Write**:
- Graph validation tests
- Edge routing priority tests
- Node type discrimination

---

#### Task 1.1.2: Implement Message System ✅ **COMPLETED**

**Objective**: Create the complete message type hierarchy for conversation history.

**Acceptance Criteria**:
- [x] `MessageType` enum: `Human`, `AI`, `Tool`, `System`, `Function`, `Remove`
- [x] `ImageDetail` enum: `Auto`, `Low`, `High`
- [x] `MessageContent` class supporting both simple string and complex content array
- [x] `MessageContentImageUrl`, `MessageContentText`, `MessageContentToolUse` classes
- [x] `MessageContentComplex` abstract record discriminator
- [x] `BaseMessage` abstract base class with: `Id`, `Timestamp`, `Type`, `Content`, `Name`, `AdditionalKwargs`, `ResponseMetadata`
- [x] Concrete message classes: `HumanMessage`, `AIMessage`, `ToolMessage`, `SystemMessage`, `FunctionMessage`, `RemoveMessage`
- [x] `ToolCall` and `InvalidToolCall` classes for LLM tool invocations
- [x] `TokenUsage` class with: `InputTokens`, `OutputTokens`, `TotalTokens`, `InputTokenDetails`, `OutputTokenDetails`
- [x] `TokenDetailedUsage` class for audio, cache, reasoning token breakdowns
- [x] `AIMessage` includes collections for `ToolCalls`, `InvalidToolCalls`, and `UsageMetadata`

**Reference**: [Architecture 2.2](02-architecture.md#22-message-system) and [Specs 3.7-3.8](01-specs.md#37-message-types)

**Implementation Notes**:
- Support polymorphic serialization for JSON round-tripping (prepare custom converters)
- Ensure backward compatibility for legacy message types
- Design for streaming token usage updates
- Messages are immutable once created

**Tests to Write**:
- Message creation and validation
- Polymorphic serialization/deserialization
- Token usage calculations
- Message content type switching

---

###  **Phase 1.2: Execution Context and State Management** ✅ **COMPLETED**
*Dependencies: Phase 1.1*  
*Estimated Effort: 2 days*

#### Task 1.2.1: Implement ExecutionContext and VariableScope ✅ **COMPLETED**

**Objective**: Build the runtime state container for graph execution.

**Acceptance Criteria**:
- [x] `ExecutionContext` class with: `ExecutionId`, `ThreadId`, `GraphId`, `CurrentNodeId`, `GlobalVariables` (READ-ONLY), `Variables` (VariableScope - READ-ONLY), `Messages` (READ-ONLY), `NodeExecutionHistory` (READ-ONLY), `ServiceProvider`, `Logger`, `ActivitySource`, `CurrentActivity`, `CancellationToken`, `Metrics`, `StartedAt`, `Metadata` (READ-ONLY)
- [x] **IMMUTABILITY**: All collections are read-only (IReadOnlyDictionary, IReadOnlyList)
- [x] `WithUpdates()` method for creating new ExecutionContext with merged state
- [x] `VariableScope` class managing hierarchical variables (global + node-scoped) - read-only access
- [x] Methods on `VariableScope`: `GetGlobal()`, `SetGlobal()`, `GetNodeVariable()`, `SetNodeVariable()`, `TryGetVariable()` (with fallback)
- [x] `ExecutionMetrics` class tracking: `NodesExecuted`, `TotalTokensUsed`, `TotalEstimatedCost`, `TotalDuration`, `NodeTypeCounters`
- [x] Thread-safe access to ExecutionContext state (using ConcurrentDictionary for VariableScope)
- [x] Support for serialization/deserialization for checkpoint restoration (ExecutionContextSnapshot)
- [x] Static `FromSnapshot()` method for creating ExecutionContext from checkpoint

**Reference**: [Architecture 2.3](02-architecture.md#23-execution-context) and [Specs 6.2](01-specs.md#62-execution-context)

**Implementation Notes**:
- Use `ConcurrentDictionary` for thread-safe variable storage in VariableScope
- **ExecutionContext is IMMUTABLE** - nodes cannot mutate context directly
- Nodes return state changes via NodeResult (messages + variables)
- GraphExecutionEngine creates new ExecutionContext using `WithUpdates()` method
- This enables proper time-travel debugging and thread-safety
- Design VariableScope for efficient nested scope resolution with read-only access

**Tests to Write**:
- Variable scoping (global vs node-level)
- Concurrent variable access
- Metric accumulation
- Checkpoint serialization round-trips

---

### **Phase 1.3: Special Node Types**
*Dependencies: Phase 1.1, 1.2*  
*Estimated Effort: 1.5 days*

#### Task 1.3.1: Implement StartNode and EndNode

**Objective**: Create entry and exit point nodes for the graph.

**Acceptance Criteria**:
- [ ] `StartNode` sealed class inheriting `Node`
  - `NodeType` returns `NodeType.Start`
  - `ExecuteAsync`: Returns NodeResult with initial HumanMessage from ExecutionContext.GlobalVariables["input"]
  - **DOES NOT mutate context** - returns messages to be appended by execution engine
  - No outgoing routing logic (always passes to first edge)
- [ ] `EndNode` sealed class inheriting `Node`
  - `NodeType` returns `NodeType.End`
  - `ExecuteAsync`: Returns NodeResult with final output captured from last message
  - No outgoing edges allowed (validation in Graph)
- [ ] Graph validation ensures: StartNode at entry, EndNode at exit, no other entry/exit points

**Reference**: [Architecture 2.1.2](02-architecture.md#212-node) - Special Node Types

**Implementation Notes**:
- StartNode should be instantiated automatically by GraphBuilder
- EndNode should be instantiated automatically by GraphBuilder
- Both nodes accept arbitrary input/output (flexible serialization)
- StartNode signature: takes initial input from ExecutionContext.GlobalVariables["input"]

**Tests to Write**:
- StartNode message initialization
- EndNode output capture
- Graph validation for proper entry/exit
- Input/output data flow

---

#### Task 1.3.2: Implement AINode with Executable Delegate

**Objective**: Create a flexible node type accepting user-defined execution logic.

**Acceptance Criteria**:
- [ ] `AINode` class inheriting `Node`
  - `NodeType` returns `NodeType.AI`
  - `ExecutionLogic` property accepting `Func<IExecutionContext, CancellationToken, Task<NodeResult>>`
  - `ExecuteAsync`: Delegates to user-provided ExecutionLogic
  - **User logic returns NodeResult with messages/variables** - NOT mutating context
  - Middleware pipeline applies before/after execution
- [ ] Support for any C# execution logic (LLM calls, database queries, custom algorithms)
- [ ] Proper error handling with exception propagation to NodeResult

**Reference**: [Architecture 2.1.2](02-architecture.md#212-node) - AINode and [Specs 3.3](01-specs.md#33-logic-node)

**Implementation Notes**:
- ExecutionLogic should handle its own LLM provider instantiation (passed via DI in ExecutionContext)
- Configuration passed via ExecutionContext, not constructor
- Support async/await throughout for I/O operations
- Cancellation token must be honored for graceful shutdown

**Tests to Write**:
- Basic lambda execution
- Async operation support
- Error handling and exception propagation
- Cancellation token propagation
- NodeResult contains messages and variables (not context mutation)
- Verifying context remains unchanged after node execution

---

### **Phase 1.4: Checkpoint System**
*Dependencies: Phase 1.1, 1.2, 1.3*  
*Estimated Effort: 1.5 days*

#### Task 1.4.1: Implement Checkpoint Model and InMemoryCheckpointStore

**Objective**: Create state capture and recovery mechanism for MVP.

**Acceptance Criteria**:
- [ ] `Checkpoint` class with: `Id`, `ExecutionId`, `GraphId`, `CreatedAt`, `Messages` (List<BaseMessage>), `ExecutionContext` (serialized)
- [ ] `CheckpointRetentionPolicy` class with: `MaxCheckpointsPerExecution`, `MaxAge`, `MaxTotalSizeBytes`
- [ ] `ICheckpointStore` interface with methods:
  - `SaveAsync(Checkpoint, CancellationToken)`
  - `LoadAsync(string checkpointId, CancellationToken) → Task<Checkpoint>`
  - `ListAsync(string executionId, CancellationToken) → Task<IReadOnlyList<Checkpoint>>`
  - `DeleteAsync(string checkpointId, CancellationToken)`
  - `PruneAsync(CheckpointRetentionPolicy, CancellationToken) → Task<int>`
- [ ] `InMemoryCheckpointStore` implementation for MVP
  - Thread-safe in-memory dictionary storage
  - No serialization (direct object references)
  - Automatic pruning based on retention policy
- [ ] Checkpoint serialization support (System.Text.Json preparation)

**Reference**: [Architecture 2.4](02-architecture.md#24-checkpoint-system) and [Specs 4](01-specs.md#4-checkpoint-system)

**Implementation Notes**:
- In-memory store for MVP (sufficient for local testing)
- Store ExecutionContext directly (no serialization needed for in-memory)
- Implement deep copy semantics when saving checkpoints
- Pruning should be configurable but pruning is optional for MVP

**Tests to Write**:
- Checkpoint save/load round-trip
- Execution context restoration
- List operations and filtering
- Retention policy enforcement
- Concurrent checkpoint operations
- Message history preservation

---

### **Phase 1.5: Logging and Basic Observability**
*Dependencies: Phase 1.2*  
*Estimated Effort: 1 day*

#### Task 1.5.1: Implement Structured Logging

**Objective**: Add comprehensive logging to execution pipeline.

**Acceptance Criteria**:
- [ ] `ILogger<T>` integration via Microsoft.Extensions.Logging.Abstractions
- [ ] Structured logging with correlation IDs (ExecutionId)
- [ ] Log levels: Debug (node entry/exit), Information (execution stages), Warning (recoverable errors), Error (execution failures)
- [ ] Logged events:
  - Execution started/completed
  - Node execution started/completed (with node type, duration)
  - Message added to history
  - Checkpoint created
  - Errors with full context
- [ ] Execution context passes Logger to nodes for user-level logging
- [ ] Proper cleanup of resources in finally/catch blocks

**Reference**: [Specs 7.1.2](01-specs.md#712-timing-capture)

**Implementation Notes**:
- Use semantic logging: structured key-value pairs instead of string interpolation
- Include execution/node/checkpoint IDs in all relevant logs
- Consider performance: don't log in hot paths excessively
- Prepare for OpenTelemetry integration later (Phase 4)

**Tests to Write**:
- Logging output verification
- Correlation ID propagation
- Log level filtering
- No sensitive data in logs

---

### **Phase 1.6: Graph Builder (Fluent API)**
*Dependencies: Phase 1.1, 1.3*  
*Estimated Effort: 1.5 days*

#### Task 1.6.1: Implement GraphBuilder with Fluent API

**Objective**: Create fluent interface for graph construction.

**Acceptance Criteria**:
- [ ] `GraphBuilder` class with:
  - Constructor taking graph name
  - `AddStartNode()` - Creates and adds StartNode (automatic, called first)
  - `AddAINode(id, name, executionLogic, configure)` - Adds AINode with optional configuration
  - `AddEndNode(id)` - Creates and adds EndNode (called last)
  - `AddEdge(id, sourceNodeId, targetNodeId, routingFunction, priority)` - Adds edge with routing
  - `Build() → Graph` - Validates and returns immutable graph
- [ ] `AINodeBuilder` for node-specific configuration (middleware attachment for Phase 2)
- [ ] Graph validation on Build():
  - StartNode exists and is entry point
  - EndNode exists and is exit point
  - No orphaned nodes
  - All edge source/target nodes exist
  - Return meaningful validation errors

**Reference**: [Architecture 2.9](02-architecture.md#29-graph-builder-fluent-api) and [Specs 6.1](01-specs.md#61-execution-flow)

**Implementation Notes**:
- Builder should be fluent (return self for chaining)
- Validation happens in Build(), not during Add operations (fail fast on Build)
- Store intermediate state in dictionaries, convert to immutable collections in Build()
- Support optional edge routing functions (null = always true)

**Tests to Write**:
- Fluent API chaining
- Graph construction and validation
- Error cases (missing start/end, orphaned nodes, invalid edges)
- Builder state mutations don't affect built graph
- Multiple graph construction from same builder instance

---

### **Phase 1.7: LLM Provider Abstraction (OpenAI)**
*Dependencies: Phase 1.2*  
*Estimated Effort: 3-4 days*

#### Task 1.7.1: Implement ILLMProvider Interface and OpenAI Provider

**Objective**: Create abstraction for LLM API calls with OpenAI as first implementation.

**Acceptance Criteria**:
- [ ] `ILLMProvider` interface with:
  - `ProviderType` property returning `LLMProvider` enum
  - `CompletionAsync(LLMRequest, CancellationToken) → Task<LLMResponse>`
  - `StreamCompletionAsync(LLMRequest, CancellationToken) → IAsyncEnumerable<LLMStreamChunk>` (basic implementation for MVP)
- [ ] `LLMProvider` enum: `OpenAI`, `Ollama`
- [ ] `LLMRequest` class with: `Messages` (List<BaseMessage>), `SystemPrompt`, `Parameters` (Dictionary)
- [ ] `LLMResponse` class with: `Content`, `TokenUsage`, `ToolCalls`, `InvalidToolCalls`, `Metadata`
- [ ] `LLMStreamChunk` class with: `Delta`, `IsFirstToken`, `IsComplete`, `TokenUsage`
- [ ] `OpenAIProvider` implementation:
  - HTTP client for API calls to OpenAI endpoints
  - Support for gpt-4 and gpt-3.5-turbo models
  - Proper error handling (rate limits, network errors, API errors)
  - Token usage tracking from API responses
  - Async/await throughout
  - Timeout configuration
  - Support for tool calls in response (prepare for Phase 2)

**Reference**: [Architecture 2.7](02-architecture.md#27-llm-provider-abstraction)

**Implementation Notes**:
- Use HttpClientFactory for pooled connections
- API key from configuration/environment
- Support message content serialization to OpenAI format
- Track token usage from response headers
- Implement proper retry logic (via Polly or manual with backoff)
- Streaming support can be basic (accumulate chunks for MVP)
- Tool call parsing (for Phase 2 ToolNode)

**Tests to Write**:
- OpenAI API integration (mock HTTP)
- Request/response serialization
- Token usage calculation
- Error handling and retries
- Timeout behavior
- Tool call parsing
- Message content conversion (text, images, tools)

---

#### Task 1.7.2: Add OllamaProvider for Local Model Support

**Objective**: Create minimal Ollama provider for local LLM testing.

**Acceptance Criteria**:
- [ ] `OllamaProvider` implementation:
  - HTTP client for local Ollama endpoints (default: http://localhost:11434)
  - Support for any Ollama model
  - `CompletionAsync` implementation
  - `StreamCompletionAsync` support (simpler than OpenAI)
  - Basic error handling
- [ ] Configuration: Base URL, model name, timeout
- [ ] Token usage estimation (approximate based on word count if not provided)

**Reference**: [Architecture 2.7](02-architecture.md#27-llm-provider-abstraction) - LLM Provider Abstraction

**Implementation Notes**:
- Ollama API is simpler than OpenAI
- Token counts may not be provided (estimate as word count * 1.3)
- Support streaming and non-streaming
- No API key needed

**Tests to Write**:
- Local Ollama integration (mock or real instance)
- Request/response format
- Streaming behavior
- Fallback for missing token counts

---

### **Phase 1.8: Execution Engine**
*Dependencies: Phase 1.1, 1.2, 1.3, 1.4, 1.6, 1.7*  
*Estimated Effort: 2-3 days*

#### Task 1.8.1: Implement GraphExecutionEngine Core

**Objective**: Build the main execution orchestrator.

**Acceptance Criteria**:
- [ ] `GraphExecutionEngine` class with:
  - Constructor accepting `ICheckpointStore`, `ILogger`, `ActivitySource`, `EngineConfiguration`
  - `ExecuteAsync(Graph, ExecutionContext, CancellationToken) → Task<ExecutionResult>`
  - `RestoreFromCheckpointAsync(checkpointId, CancellationToken) → Task<ExecutionContext>`
  - `ReplayFromCheckpointAsync(checkpointId, CancellationToken) → Task<ExecutionResult>`
- [ ] `ExecutionResult` class with: `Success`, `Context`, `Messages`, `FinalOutput`, `Duration`, `Error`
- [ ] `EngineConfiguration` class with: `MaxConcurrentExecutions`, `DefaultNodeTimeout`, `CheckpointStrategy`, `CheckpointRetentionPolicy`, `EnableTelemetry`, `TelemetryConfig`
- [ ] `CheckpointStrategy` enum: `Never`, `BeforeEachNode`, `OnDemand`, `OnErrorOnly`
- [ ] `NodeExecutor` class for executing individual nodes with middleware pipeline
- [ ] `EdgeRouter` class for evaluating edges and determining next node

**Reference**: [Architecture 2.6](02-architecture.md#26-execution-engine) and [Specs 6.1](01-specs.md#61-execution-flow)

**Implementation Notes**:
- ExecuteAsync orchestrates the main loop: Initialize → Loop until terminal node → Return result
- **After each node execution**: Create new ExecutionContext using `context.WithUpdates(nodeResult.Messages, nodeResult.Variables, recordNodeExecution)`
- Support checkpoint creation based on strategy (checkpoint the NEW context after updates)
- Handle cancellation token throughout
- Proper error handling with ExecutionResult containing error info
- Metrics accumulation in ExecutionContext (Metrics object is mutable for performance)
- Thread-safe for concurrent execution up to MaxConcurrentExecutions

**Tests to Write**:
- Simple linear execution (StartNode → AINode → EndNode)
- Execution flow and node ordering
- Cancellation token propagation
- Timeout enforcement
- Error handling and result propagation
- Metrics collection
- Checkpoint creation/restoration
- Context passing through nodes

---

#### Task 1.8.2: Implement NodeExecutor

**Objective**: Execute individual nodes with middleware support.

**Acceptance Criteria**:
- [ ] `NodeExecutor` class with:
  - `ExecuteAsync(Node, ExecutionContext, CancellationToken) → Task<NodeResult>`
  - Middleware pipeline execution: OnBeforeExecute → ExecuteAsync → OnAfterExecute
  - Error handling with OnError middleware hook
  - Duration tracking and result wrapping
- [ ] Support for all node types (StartNode, EndNode, AINode)
- [ ] Exception handling with proper error propagation
- [ ] Timeout enforcement per node (from EngineConfiguration)

**Reference**: [Architecture 2.6](02-architecture.md#26-execution-engine) - NodeExecutor

**Implementation Notes**:
- Middleware is empty for MVP (prepare for Phase 3)
- Handle exceptions and wrap in NodeResult
- Track execution duration
- Propagate cancellation token
- **Returns NodeResult** - does NOT mutate ExecutionContext
- NodeExecutor is responsible for timing and error wrapping only
- Actual state merging happens in GraphExecutionEngine using `context.WithUpdates()`

**Tests to Write**:
- Node execution with result capture
- Timeout handling
- Exception handling and error result
- Metrics (duration, status)
- Context state after execution

---

#### Task 1.8.3: Implement EdgeRouter

**Objective**: Evaluate edges and determine next node.

**Acceptance Criteria**:
- [ ] `EdgeRouter` class with:
  - `DetermineNextEdgeAsync(Node, ExecutionContext, CancellationToken) → Task<Edge>`
  - Get all outgoing edges from current node
  - Evaluate routing functions in priority order (lower = first)
  - Return first edge with routing function returning true
  - Throw/log if no edge activates (error condition or end of execution)
- [ ] Support for default edge (always-true routing function)
- [ ] Logging of routing decisions

**Reference**: [Architecture 2.6](02-architecture.md#26-execution-engine) - EdgeRouter and [Specs 3.8](01-specs.md#38-edge-routing)

**Implementation Notes**:
- Routing functions receive full ExecutionContext for decision-making
- Null routing function = always true (used for default edge)
- Priority-based evaluation
- Log which edge was selected for debugging

**Tests to Write**:
- Multiple edge routing with priorities
- Routing function decision logic
- Default edge fallback
- Error when no edge activates
- Logging of routing decisions

---

### **Phase 1.9: Dependency Injection and Configuration**
*Dependencies: Phase 1.2, 1.7, 1.8*  
*Estimated Effort: 1 day*

#### Task 1.9.1: Implement Service Registration and Configuration

**Objective**: Set up DI container and configuration for the engine.

**Acceptance Criteria**:
- [ ] `WolfEngineOptions` class with: `Engine`, `Telemetry`, `Logging`, `LLMProviders`
- [ ] `EngineConfiguration` (from 1.8.1)
- [ ] `TelemetryConfiguration` class with: `Enabled`, `ServiceName`, `Exporters`, `SamplingRate`
- [ ] `ExporterConfiguration` class with: `Type`, `Settings`
- [ ] `ExporterType` enum: `Console`, `OTLP`, `Jaeger`, `Prometheus`
- [ ] `LoggingConfiguration` class with: `Enabled`, `MinimumLevel`, `IncludeScopes`, `CategoryLevels`
- [ ] `LLMProviderConfiguration` class with: `Provider`, `ApiKey`, `Endpoint`, `Timeout`, `RetryPolicy`
- [ ] Extension method `AddWolfAI(this IServiceCollection, Action<WolfEngineOptions>)` for service registration:
  - Register `GraphExecutionEngine` as singleton
  - Register `ICheckpointStore` as singleton (default: InMemoryCheckpointStore)
  - Register LLM providers: OpenAI, Ollama
  - Register built-in tools (WebSearchTool, CalculatorTool - stubs for MVP)
  - Register logging configuration
  - Register options pattern configuration
- [ ] Support configuration from appsettings.json and environment variables
- [ ] Fluent configuration builder

**Reference**: [Architecture 2.9-2.10](02-architecture.md#210-dependency-injection-setup)

**Implementation Notes**:
- Prepare for Phase 2 tool registration
- Logging configuration should integrate with ILoggingBuilder
- Support multiple LLM provider instances
- Timeout defaults: 30sec for tools, 5min for node execution, 60sec for LLM calls
- Retry policy defaults: 3 attempts with exponential backoff

**Tests to Write**:
- DI container registration
- Configuration loading from various sources
- Singleton behavior
- LLM provider factory
- Configuration validation

---

### **Phase 1.10: Integration Tests and MVP Validation**
*Dependencies: All Phase 1 tasks*  
*Estimated Effort: 2 days*

#### Task 1.10.1: Create MVP Integration Test Suite

**Objective**: Validate end-to-end graph execution with OpenAI.

**Acceptance Criteria**:
- [ ] Integration test: LinearWorkflow
  - Graph: StartNode → AINode (OpenAI call) → EndNode
  - Input: Simple prompt ("Hello, what's your name?")
  - Verify: AIMessage in output, token usage tracked, execution completed successfully
- [ ] Integration test: WithCheckpoint
  - Graph: StartNode → AINode → EndNode
  - Create execution context
  - Execute node, capture checkpoint
  - Restore from checkpoint
  - Continue execution
  - Verify: Consistent results
- [ ] Integration test: CustomLogic
  - Graph: StartNode → AINode (custom C# logic) → EndNode
  - Custom logic: Adds computed variable, logs message
  - Verify: Message added to history, variable accessible in context
- [ ] Integration test: ExecutionMetrics
  - Execute workflow
  - Verify: NodesExecuted, TotalDuration, NodeTypeCounters tracked
- [ ] Test configuration loading from appsettings.json
- [ ] Test logging output contains expected information

**Reference**: [Specs 1.2](01-specs.md#12-target-use-cases) - Target Use Cases

**Implementation Notes**:
- Use mock HTTP client for tests (Moq or TestServer)
- Create sample appsettings.json for testing
- Include timeout and error scenarios
- Document expected behavior for next phases

**Tests to Write**:
- End-to-end graph execution
- OpenAI API mocking
- Checkpoint round-trip
- Metrics collection
- Configuration integration
- Logging verification

---

## Implementation Roadmap

### Week 1
- **Day 1-2**: Phase 1.1 (Graph, Node, Edge models)
- **Day 2-3**: Phase 1.2 (Message system - complex task)
- **Day 3-4**: Phase 1.3 (ExecutionContext, VariableScope)
- **Day 4-5**: Phase 1.4 (StartNode, EndNode, AINode)

### Week 2
- **Day 1**: Phase 1.5 (Checkpoint system)
- **Day 2**: Phase 1.6 (Structured logging)
- **Day 2-3**: Phase 1.7 (GraphBuilder)
- **Day 3-4**: Phase 1.8 (OpenAI provider)

### Week 3
- **Day 1-2**: Phase 1.9 (GraphExecutionEngine)
- **Day 2-3**: Phase 1.10 (NodeExecutor, EdgeRouter)
- **Day 4**: Phase 1.11 (DI and configuration)
- **Day 5**: Phase 1.12 (Integration tests and validation)

---

## Testing Strategy for MVP

### Unit Tests
- **Per Component**: ~5-10 tests per major class
- **Focus**: Behavior, error handling, edge cases
- **Mocking**: HttpClient for LLM providers, ILogger, ICheckpointStore
- **Tools**: xUnit, Moq, FluentAssertions

### Integration Tests
- **Workflow Execution**: Full graph from start to end
- **Checkpoint Lifecycle**: Save, restore, replay
- **Configuration**: Multiple sources, environment overrides
- **External Services**: Mocked OpenAI and Ollama endpoints

### Coverage Goals
- **Minimum**: 80% code coverage
- **Critical Paths**: 100% coverage (execution, checkpoints, message history)
- **Happy Path**: Thoroughly tested basic workflows

---

## Sample Project: WolfAI.Samples

### Overview

Located in `src/WolfAI/Samples/`, the WolfAI.Samples console application demonstrates how to use the WolfAI.Core APIs for basic graph construction and execution without requiring external LLM services. This sample serves as both documentation and an integrated test for the core APIs.

### Running the Sample

```bash
cd src/WolfAI
dotnet run --project Samples/WolfAI.Samples.csproj
```

### What the Sample Demonstrates

The sample creates a simple workflow graph with the following structure:

```
StartNode
    ↓
ProcessingNode (transforms input to uppercase)
    ↓
DecisionNode (evaluates success flag)
    ├→ SuccessEndNode (if processed_success = true)
    └→ FailureEndNode (if processed_success ≠ true)
```

**Key Concepts Shown**:

1. **Graph Construction**: Building a Graph with nodes and edges
2. **Node Types**: Custom implementation of `StartNode`, `AINode` (ProcessingNode, DecisionNode), and `EndNode`
3. **Node Execution**: Running nodes asynchronously with `ExecuteAsync()`
4. **ExecutionContext**: Creating and immutably updating context with `WithUpdates()`
5. **Routing Logic**: Evaluating edges with `routingFunction` predicates for conditional routing
6. **Snapshots**: Creating execution context snapshots for checkpointing
7. **State Management**: Tracking node execution history and global variables

### Sample Components

- **SimpleStartNode**: Extracts initial input from `GlobalVariables["input"]`
- **ProcessingNode**: Transforms input (uppercase) and sets variables for routing
- **DecisionNode**: Makes routing decisions based on context variables
- **SimpleEndNode**: Terminal node that marks successful completion

### Example Output

The sample produces console output showing:
- Graph structure and configuration
- Step-by-step node execution with timing
- Edge routing decisions
- Execution summary with history and final state
- Context snapshot data

### Using the Sample for Integration Testing

To extend this sample for your own integration tests:

1. **Custom Nodes**: Implement additional `Node` subclasses with your own logic
2. **Conditional Routing**: Add `routingFunction` predicates to edges to test branching
3. **Variable Tracking**: Examine `context.GlobalVariables` to verify state changes
4. **Execution History**: Check `context.NodeExecutionHistory` to validate execution order
5. **Snapshots**: Use `context.CreateSnapshot()` to verify state capture

### Files

- [Program.cs](../src/WolfAI/Samples/Program.cs) - Complete sample implementation with detailed comments
- [WolfAI.Samples.csproj](../src/WolfAI/Samples/WolfAI.Samples.csproj) - Project configuration

---

## Definition of Done for MVP

- [ ] All Phase 1 tasks completed and merged
- [ ] Unit test coverage ≥ 80%
- [ ] Integration tests pass (happy path + error scenarios)
- [ ] Architecture document up-to-date and accurate
- [ ] Code follows C# style guidelines (StyleCop)
- [ ] All public APIs documented with XML comments
- [ ] Sample application demonstrating MVP (in samples/)
- [ ] Git history clean with meaningful commits

---

## Technical Debt & Future Considerations

**Phase 2 Preparation**:
- Message system supports tool call information
- GraphBuilder prepared for ToolNode addition
- Middleware pipeline prepared (empty in MVP)
- Edge routing fully implemented

**Not in MVP**:
- [ ] Middleware system (Phase 3)
- [ ] Advanced observability (Phase 4)
- [ ] Persistent checkpoint stores (Phase 5)
- [ ] Additional LLM providers (Phase 5)
- [ ] Event streaming
- [ ] Parallel execution
- [ ] Web API / hosting

---

**Last Updated**: January 29, 2026  
**Created For**: Senior C# Developer Implementation
