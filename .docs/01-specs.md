# WolfAI - Agentic AI Engine Specification

## 1. Overview

WolfAI is a C# engine designed to manage and orchestrate agentic AI applications through a graph-based execution model. The engine enables developers to build complex AI workflows by composing nodes that can call LLMs, execute tools, or run custom C# logic, all connected through directed edges.

### 1.1 Core Objectives

- **Graph-Based Execution**: Define AI workflows as directed graphs with nodes and edges
- **Flexible Node Types**: Support multiple execution patterns (LLM calls, tool invocations, custom logic)
- **Time-Travel Debugging**: Enable checkpoint-based rollback and replay capabilities
- **Production-Ready Observability**: Built-in OpenTelemetry integration for distributed tracing
- **Comprehensive Logging**: Structured logging for debugging and monitoring

### 1.2 Target Use Cases

- Multi-agent AI systems
- Complex decision-making workflows
- AI-powered automation pipelines
- Conversational AI with tool usage
- Debuggable and traceable AI applications

---

## 2. Architecture

### 2.1 Graph Model

The execution model is based on a **Directed Acyclic Graph (DAG)** with optional cycles for iterative workflows.

```
┌─────────────┐
│   Engine    │
└──────┬──────┘
       │
       ├──> Graph Definition
       │    ├── Nodes (execution units)
       │    └── Edges (control flow)
       │
       ├──> Execution Context
       │    ├── State
       │    └── Variables
       │
       └──> Checkpoint Manager
            ├── State Snapshots
            └── History Log
```

### 2.2 Core Components

#### 2.2.1 Graph
- **Responsibility**: Container for nodes and edges
- **Properties**:
  - `Id`: Unique identifier
  - `Name`: Human-readable name
  - `Nodes`: Collection of execution nodes
  - `Edges`: Collection of connections between nodes
  - `EntryPoint`: Initial node for execution
  - `Variables`: Shared state across nodes

#### 2.2.2 Node
- **Responsibility**: Single unit of execution
- **Properties**:
  - `Id`: Unique identifier
  - `Name`: Human-readable name
  - `Type`: Node type (LLM, Tool, Logic)
  - `Configuration`: Node-specific settings
  - `InputSchema`: Expected input structure
  - `OutputSchema`: Expected output structure

#### 2.2.3 Edge
- **Responsibility**: Define execution flow between nodes with routing logic
- **Properties**:
  - `Id`: Unique identifier
  - `SourceNodeId`: Origin node
  - `TargetNodeId`: Destination node
  - `RoutingFunction`: Custom user function to determine if edge is activated
  - `Priority`: Order for evaluating edges when multiple exist from same source

---

## 3. Node Types

All node types can have multiple outgoing edges with custom routing functions to determine which next node is executed. The routing decision is made dynamically based on the complete execution context.

### 3.1 LLM Node

Executes calls to Large Language Models.

**Configuration Options:**
- Provider selection (OpenAI, Azure, Anthropic, Ollama, Local, Custom)
- Model selection
- Temperature and token limits
- System prompts and user prompt templates
- Response format (text, JSON, structured)

**Capabilities:**
- Support multiple LLM providers through abstraction
- Template-based prompt construction
- Streaming support
- Response parsing and validation
- Automatic timing metrics (API call duration, first token latency)
- Token usage tracking (input/output tokens per request)
- Cost estimation based on token usage

### 3.2 Tool Node

Executes LLM tool calls or function invocations.

**Configuration Options:**
- Tool name and registration
- Parameter mapping and templating
- Timeout settings
- Retry policies and backoff strategies

**Capabilities:**
- Built-in tool library (web search, calculations, data access)
- Custom tool registration
- Async execution support
- Error handling and retries

### 3.3 Logic Node

Executes custom compiled C# code as node.

### 3.4 Conditional Node

Routes execution to different nodes based on custom routing logic.

**Routing Mechanism:**
- A node can have multiple outgoing edges to different nodes
- Each edge has a custom routing function (user-defined)
- Routing function receives entire execution context (including messages, variables, state)
- Only one edge is activated per node execution
- Engine evaluates edges by priority until one activates
- First edge with `RoutingFunction` returning true is taken

### 3.5 Loop Node

Repeats execution for iterations or collections.

**Configuration Options:**
- Iteration type (Count, Collection, While)
- Maximum iteration limits
- Collection references
- Loop condition expressions

---

## 3.7 Message Types

The workflow maintains a conversation history through different message types that represent different interactions:

**HumanMessage**
- Input from the user
- Represents user requests or queries
- Added at the start of execution or when user provides new input

**AIMessage**
- Output from an LLM node to the user
- Represents the LLM's response when no tool is needed
- Contains the LLM's text response

**ToolCallMessage**
- Output from an LLM node when a tool must be invoked
- Contains the tool name and required parameters
- Signals that a tool execution node should be the next step

**ToolMessage**
- Output from a tool node with execution results
- Contains the tool response data (typically JSON)
- Provides context for subsequent LLM processing

**Example Message Flow:**
1. HumanMessage: "Tell me a pirate joke"
2. ToolCallMessage: { tool: "GetJoke", parameters: { category: "pirate" } }
3. ToolMessage: { joke: "Why is a pirate's favorite letter R? ..." }
4. AIMessage: "Here's a pirate joke: Why is a pirate's favorite letter R? ..."

---

## 3.8 Edge Routing

Every node in the graph can have multiple outgoing edges connecting to different nodes. Edge routing determines which edge (and thus which next node) is activated after current node execution completes.

**Routing Mechanism:**
- Each edge has a custom `RoutingFunction` (user-defined)
- Routing function receives entire execution context:
  - All variables (global and node-level)
  - Complete message history (HumanMessage, AIMessage, ToolCallMessage, ToolMessage interactions)
  - Node execution results and output
  - Execution state and metadata
- Routing function returns boolean: `true` to activate this edge, `false` otherwise
- At most one edge is activated per node execution
- Edges are evaluated in priority order (lower priority first)
- First edge with routing function returning `true` is taken
- If no edge routing function returns `true`, execution terminates (error or default behavior)

**Routing Function Responsibilities:**
- Inspect execution state to make routing decision
- Access message history for conversation-aware routing
- Evaluate node output and determine next step
- Support complex routing logic (not just boolean conditions)

**Example Routing Logic:**

Edge 1 to "FAQ Handler" node:
```
If last message is from AI AND message contains "faq_match"
  Then return true (take this edge)
```

Edge 2 to "Ticket Creator" node (priority 10):
```
If last message is from AI AND message contains "escalate"
  Then return true (take this edge)
```

Edge 3 to "Default Handler" node (priority 20):
```
Always return true (default/fallback edge)
```

When "Intent Classification" node completes, the engine evaluates edges by priority and uses the first edge whose routing function returns true.

---

## 4. Checkpoint System

### 4.1 Purpose

Enable time-travel debugging and state recovery by capturing execution snapshots.

### 4.2 Checkpoint Structure

Each checkpoint captures:
- Unique checkpoint identifier
- Timestamp of when checkpoint was created
- Graph identifier and current node position
- Complete variable state (deep copy)
- Execution history and steps taken
- Message history (HumanMessage, AIMessage, ToolCallMessage, ToolMessage conversations)
- Metadata about checkpoint creation

### 4.3 Checkpoint Operations

#### Create Checkpoint
- **Trigger**: Before each node execution or manually
- **Data Captured**: 
  - Current node
  - Variable state (deep copy)
  - Execution history
  - Message history (all HumanMessage, AIMessage, ToolCallMessage, ToolMessage interactions)
  - Timestamp and metadata

#### Restore Checkpoint
- **Action**: Roll back to a previous state
- **Behavior**:
  - Restore variable state
  - Reset execution position
  - Clear future history
  - Emit telemetry event

#### Replay from Checkpoint
- **Action**: Re-execute forward from a checkpoint
- **Behavior**:
  - Restore state
  - Continue execution with new decisions
  - Maintain original checkpoint for comparison

### 4.4 Storage Options

- **In-Memory**: Fast, session-only (default)
- **Persistent**: Database or file-based for long-term storage
- **Configurable Retention**: Auto-cleanup based on age/count

---

## 5. Node-Level Middleware System

### 5.1 Purpose

Node-level middleware enables intercepting and modifying node execution without changing node implementations. Middleware operates at the node execution level, allowing cross-cutting concerns like request/response transformation, message manipulation, and execution context modification.

### 5.2 Node Middleware Pipeline Architecture

```
[Message List] → [Middleware 1] → [Middleware 2] → [Middleware N] → [Node Execution] → [Middleware N] → [Middleware 2] → [Middleware 1] → [Modified Context]
```

### 5.3 Middleware Interface

Node-level middleware implements hooks for intercepting node-level operations:
- **Name**: Unique identifier
- **Priority**: Execution order (lower = earlier in before-phase, later in after-phase)
- **Before Node Execution Hook**: Invoked before node execution, can modify:
  - Message history (e.g., summarize messages)
  - Execution context variables
  - Node input parameters
- **After Node Execution Hook**: Invoked after successful execution, can modify:
  - Node output/response
  - Execution context
  - Message history
- **On LLM Request Hook** (for LLM nodes): Modify LLM request before sending:
  - Adjust prompt
  - Modify parameters
  - Add system instructions
- **On LLM Response Hook** (for LLM nodes): Modify LLM response after receiving:
  - Parse response
  - Transform output
  - Validate response
- **Error Hook**: Invoked when node execution fails

### 5.4 Built-in Node Middleware Examples

#### 5.4.1 Message Summarization Middleware
- Summarizes message history before node execution
- Reduces token count for context passing
- Maintains important information while reducing message count
- Configured with summarization strategy (sliding window, clustering, llm-based)

#### 5.4.2 Message Filtering Middleware
- Filters out irrelevant messages before execution
- Removes messages older than threshold
- Keeps only recent interactions within token budget
- Configurable retention policies

#### 5.4.3 LLM Prompt Enhancement Middleware
- Modifies LLM prompts before API calls
- Adds dynamic instructions based on context
- Injects few-shot examples
- Enforces output format specifications

#### 5.4.4 LLM Response Parsing Middleware
- Parses LLM responses into structured format
- Validates response against schema
- Extracts specific fields from response
- Handles parsing errors gracefully

#### 5.4.5 Caching Middleware
- Caches node outputs based on input hash
- Returns cached result if input matches
- Reduces redundant node executions
- Supports cache invalidation strategies

#### 5.4.6 Retry Middleware
- Implements exponential backoff retry logic
- Determines retry-able errors per node type
- Logs retry attempts
- Configurable per-node retry policies

#### 5.4.7 Token Limiting Middleware
- Monitors token usage before LLM calls
- Enforces message history token budgets
- Truncates context if exceeding limits
- Tracks token consumption

#### 5.4.8 Context Transformation Middleware
- Transforms execution context before/after node execution
- Enriches context with computed values
- Cleans up temporary variables
- Validates context state

### 5.5 Node Middleware Registration

Middleware is registered at the node level during node configuration. Multiple middleware can be chained on a single node, each with its own priority determining execution order. Middleware is applied uniformly to all executions of that node.

### 5.6 Custom Node Middleware Implementation

Custom middleware is implemented by:
1. Implementing the node middleware interface with required execution hooks
2. Assigning a name and priority
3. Implementing before-execution and after-execution hooks
4. Registering with the node

Example use cases:
- Summarizing or filtering message history before LLM calls
- Modifying LLM prompts dynamically
- Parsing and validating LLM responses
- Transforming node context based on execution results
- Implementing custom caching strategies
- Applying token/cost limits per node

### 5.7 Node Middleware Configuration

Node middleware is configured at the node level with:
- Middleware type and priority per node
- Enable/disable flags per node
- Node-specific middleware configuration (message window size, cache TTL, etc.)
- Middleware stacking for multiple interceptors on same node

---

## 6. Execution Engine

### 6.1 Execution Flow

```
1. Initialize Graph
2. Create Initial Checkpoint
3. Load Entry Node
4. Execute Node:
   a. Execute Node Logic
   b. Capture Output
   c. Update State
   d. Create Post-Execution Checkpoint
   e. Emit Telemetry
5. Determine Next Node:
   a. Get all outgoing edges from current node
   b. Evaluate each edge in priority order
   c. Execute edge's routing function with execution context
   d. Activate first edge where routing function returns true
   e. Load target node from activated edge
6. Repeat from step 4 until:
   - Terminal node reached
   - No edge routing function returns true
   - Error occurs
   - Manual stop
```

### 6.2 Execution Context

Execution context encapsulates the runtime state:
- Execution identifier and graph reference
- Variables storage (global and node-level scoped)
- Checkpoint stack for state management
- Message history list (all HumanMessage, AIMessage, ToolCallMessage, ToolMessage interactions)
- Logger and tracer references
- Cancellation token for graceful shutdown

Each node execution also maintains:
- Node-specific middleware chain for intercepting execution
- Node input parameters
- Node execution state and output

### 6.3 State Management

- **Variable Scoping**: Global (graph-level) and local (node-level)
- **Type Safety**: Support for strongly-typed variables
- **Serialization**: State must be serializable for checkpoints
- **Immutability Options**: Copy-on-write for checkpoint integrity

---

## 7. OpenTelemetry Integration

### 7.1 Distributed Tracing

**Span Hierarchy:**
```
GraphExecution (root span)
├── NodeExecution: Node1
│   ├── LLMCall: OpenAI/gpt-4
│   └── ToolInvocation: Calculator
├── NodeExecution: Node2
└── CheckpointCreation
```

**Key Attributes:**
- `wolf.graph.id`
- `wolf.graph.name`
- `wolf.node.id`
- `wolf.node.name`
- `wolf.node.type`
- `wolf.execution.id`
- `wolf.checkpoint.id`
- `wolf.llm.provider` (for LLM nodes)
- `wolf.llm.model` (for LLM nodes)
- `wolf.llm.tokens.input` (for LLM nodes)
- `wolf.llm.tokens.output` (for LLM nodes)
- `wolf.tool.name` (for Tool nodes)

**Span Events:**
- `llm.request.started` - When LLM API call begins
- `llm.first_token` - When first token received (streaming)
- `llm.request.completed` - When LLM API call completes
- `checkpoint.created` - When checkpoint is saved
- `node.error` - When node execution fails
- `state.updated` - When execution state changes

### 7.1.2 Timing Capture

All node executions automatically capture:
1. **Start Time**: Timestamp when node execution begins
2. **End Time**: Timestamp when node execution completes
3. **Duration**: Calculated as (End - Start)

For LLM nodes specifically:
1. **Request Initiation**: When HTTP request is sent to LLM API
2. **First Token Time**: When first response byte received (streaming mode)
3. **Response Complete**: When full response received
4. **Total API Duration**: Network + processing time
5. **First Token Latency**: Time from request to first token

Metrics are emitted at the end of each operation and can be queried/aggregated through the configured exporters.

### 7.2 Metrics

#### 7.2.1 Counters

**Execution Metrics:**
- `wolf.executions.total` - Total number of graph executions started
  - Labels: `graph.id`, `graph.name`, `status` (success/failure)
- `wolf.nodes.executed` - Total number of nodes executed
  - Labels: `node.type`, `node.name`, `status` (success/failure/timeout)
- `wolf.errors.total` - Total number of errors encountered
  - Labels: `error.type`, `node.type`, `severity`

**Checkpoint Metrics:**
- `wolf.checkpoints.created` - Total checkpoints created
  - Labels: `trigger` (auto/manual), `graph.id`
- `wolf.checkpoints.restored` - Total checkpoint restorations
  - Labels: `graph.id`, `reason`

**LLM Metrics:**
- `wolf.llm.requests.total` - Total LLM API requests
  - Labels: `provider`, `model`, `status` (success/failure/timeout)
- `wolf.llm.tokens.input.total` - Total input tokens consumed
  - Labels: `provider`, `model`
- `wolf.llm.tokens.output.total` - Total output tokens generated
  - Labels: `provider`, `model`
- `wolf.llm.errors.total` - Total LLM API errors
  - Labels: `provider`, `model`, `error.type` (rate_limit/timeout/invalid_request)

**Tool Metrics:**
- `wolf.tool.invocations.total` - Total tool invocations
  - Labels: `tool.name`, `status` (success/failure)
- `wolf.tool.retries.total` - Total tool retry attempts
  - Labels: `tool.name`, `reason`

#### 7.2.2 Histograms

**Duration Metrics:**
- `wolf.execution.duration` - Time to complete entire graph execution
  - Labels: `graph.id`, `graph.name`, `status`
  - Buckets: [100ms, 500ms, 1s, 5s, 10s, 30s, 60s, 120s, 300s]
- `wolf.node.duration` - Time to execute a single node
  - Labels: `node.type`, `node.id`, `graph.id`
  - Buckets: [10ms, 50ms, 100ms, 500ms, 1s, 5s, 10s, 30s]
- `wolf.llm.api.duration` - Time spent calling LLM API (request to response)
  - Labels: `provider`, `model`, `operation` (completion/chat/embedding)
  - Buckets: [100ms, 500ms, 1s, 2s, 5s, 10s, 30s, 60s]
- `wolf.llm.first_token.latency` - Time to first token in streaming responses
  - Labels: `provider`, `model`
  - Buckets: [50ms, 100ms, 200ms, 500ms, 1s, 2s, 5s]
- `wolf.tool.duration` - Time to execute tool invocation
  - Labels: `tool.name`, `status`
  - Buckets: [10ms, 50ms, 100ms, 500ms, 1s, 5s, 10s]
- `wolf.checkpoint.creation.duration` - Time to create checkpoint
  - Labels: `graph.id`, `checkpoint.size` (small/medium/large)
  - Buckets: [1ms, 5ms, 10ms, 50ms, 100ms, 500ms, 1s]
- `wolf.checkpoint.restore.duration` - Time to restore checkpoint
  - Labels: `graph.id`
  - Buckets: [1ms, 5ms, 10ms, 50ms, 100ms, 500ms, 1s]

**Size Metrics:**
- `wolf.llm.tokens.input` - Distribution of input tokens per request
  - Labels: `provider`, `model`
  - Buckets: [10, 50, 100, 500, 1000, 2000, 4000, 8000, 16000]
- `wolf.llm.tokens.output` - Distribution of output tokens per response
  - Labels: `provider`, `model`
  - Buckets: [10, 50, 100, 500, 1000, 2000, 4000, 8000]
- `wolf.state.size` - Size of execution state in bytes
  - Labels: `graph.id`
  - Buckets: [1KB, 10KB, 100KB, 1MB, 10MB, 100MB]

#### 7.2.3 Gauges

**Active Resources:**
- `wolf.executions.active` - Number of currently running graph executions
  - Labels: `graph.id`
- `wolf.nodes.active` - Number of currently executing nodes
  - Labels: `node.type`
- `wolf.llm.requests.active` - Number of in-flight LLM API requests
  - Labels: `provider`, `model`

**Storage Metrics:**
- `wolf.checkpoints.count` - Current number of stored checkpoints
  - Labels: `graph.id`, `storage.type` (memory/persistent)
- `wolf.checkpoints.size.bytes` - Total size of checkpoints in bytes
  - Labels: `storage.type`

**Queue Metrics:**
- `wolf.execution.queue.depth` - Number of executions waiting to start
- `wolf.tool.queue.depth` - Number of tool invocations queued
  - Labels: `tool.name`

#### 7.2.4 Custom Metrics

**Cost Tracking:**
- `wolf.llm.cost.estimated` - Estimated cost of LLM usage (Counter)
  - Labels: `provider`, `model`, `currency`
  - Calculated from token usage and pricing tables

**Performance Ratios:**
- `wolf.llm.tokens.per.second` - Token generation throughput (Gauge)
  - Labels: `provider`, `model`
- `wolf.execution.success.rate` - Ratio of successful executions (Gauge)
  - Labels: `graph.id`

#### 7.2.5 Metric Export Configuration

Metrics can be configured with:
- Enable/disable flags
- Export interval (in milliseconds)
- Custom histogram buckets per metric
- Aggregation temporality settings

### 7.3 Exporters

Support for:
- OTLP (OpenTelemetry Protocol)
- Jaeger
- Zipkin
- Console (development)
- Prometheus
- Custom exporters

## 8. Logging

Structured logging captures:
- Timestamp in ISO 8601 format
- Log level (Trace, Debug, Information, Warning, Error, Critical)
- Descriptive message
- Execution and graph identifiers
- Node information and type
- Duration metrics
- Trace and span IDs for correlation

Microsoft.Extensions.Logging (primary)

---

## 9. Error Handling

### 9.1 Error Types

- **ValidationError**: Invalid graph/node configuration
- **ExecutionError**: Runtime errors during node execution
- **TimeoutError**: Exceeded time limits
- **ResourceError**: Missing resources or permissions

### 9.2 Error Recovery

- **Retry Policies**: Configurable per node type
- **Fallback Nodes**: Alternative execution paths
- **Circuit Breaker**: Prevent cascading failures
- **Graceful Degradation**: Continue with partial results

### 9.3 Checkpoint Integration

- Auto-checkpoint before risky operations
- Rollback to last known good state on critical errors
- Error state captured in checkpoints for debugging

---

## 10. Configuration

### 10.1 Engine Configuration

Engine configuration includes:
- Maximum concurrent executions
- Default timeouts and limits
- Checkpoint strategy (Before Each Node, On Demand, Never)
- Checkpoint retention policies (max count, max age)
- Telemetry settings (enabled, exporters, sample rates)
- Logging configuration (min level, providers, structured logging)

### 10.2 Graph Definition Format

Graphs are always defined programmatically using C# and the WolfAI fluent API. This provides:
- **Type-safe**: Full C# compiler support and IntelliSense
- **Efficient**: Direct API calls without parsing overhead
- **Flexible**: Can use C# logic for dynamic graph construction
- **Testable**: Graphs can be unit tested directly as C# code
- **Debuggable**: Full debugging support within IDE

Graph construction uses a fluent builder API for clean, readable code:

**Example Structure:**
```
var graph = new GraphBuilder("MyWorkflow")
    .AddLLMNode("classifier", ...)
        .AddEdge("classifierTOFaq", routingFunction: ctx => IsSimpleQuestion(ctx))
            .ConnectTo("faqNode")
        .AddEdge("classifierToTicket", routingFunction: ctx => NeedsTicket(ctx))
            .ConnectTo("ticketNode")
    .AddToolNode("faqNode", ...)
    .AddToolNode("ticketNode", ...)
    .Build();
```

Node middleware and routing functions are defined as standard C# methods/lambdas, providing full programming language capabilities.

---

## 14. Development Roadmap

### Phase 1: Core Engine (MVP)
- [ ] Graph model and execution engine
- [ ] Basic node types (LLM, Logic, Tool)
- [ ] In-memory checkpoints
- [ ] Console logging

### Phase 2: Observability
- [ ] OpenTelemetry integration
- [ ] Structured logging with Serilog
- [ ] Metrics and exporters

### Phase 3: Advanced Features
- [ ] Persistent checkpoints
- [ ] Parallel execution
- [ ] Advanced node types (Loop, Conditional)
- [ ] Graph validation

### Phase 4: Production Readiness
- [ ] Security hardening
- [ ] Performance optimization
- [ ] High availability support
- [ ] Comprehensive documentation

---

## 16. Example Use Cases

### 16.1 Simple Tool Invocation Workflow

User request: "Tell me a pirate joke"

**Workflow Diagram:**
```
                    ┌─────────────┐
                    │   START     │
                    │ HumanMessage│
                    └──────┬──────┘
                           │
                           ▼
                    ┌─────────────┐
                    │  LLM Node   │
                    │  w/ Tools   │
                    └──────┬──────┘
                           │
                           ▼
                  ┌──────────────────┐
                  │  API Jokes Tool  │
                  │  (Tool Invocation)
                  └─────────┬────────┘
                            │
                            ▼
                    ┌─────────────┐
                    │  LLM Node   │
                    │ Formatter   │
                    └──────┬──────┘
                           │
                           ▼
                    ┌─────────────┐
                    │    END      │
                    │ AIMessage   │
                    └─────────────┘
```

**Execution Flow:**

1. **START Node**: HumanMessage added to context: "Tell me a pirate joke"

2. **LLM with Tools Node**: 
   - LLM receives system prompt + HumanMessage + list of available tools
   - LLM determines it should use the "GetJoke" tool
   - Creates ToolCallMessage for "API Jokes" with parameters
   - Adds ToolCallMessage to history

3. **API Jokes Tool Node**:
   - Executes API call to jokes service with category="pirate"
   - Returns JSON response: `{ "joke": "Why is a pirate's favorite letter R? ... " }`
   - Adds ToolMessage to history with the joke data

4. **LLM Formatter Node**:
   - Receives execution context with:
     - All previous messages (HumanMessage, ToolCallMessage, ToolMessage)
     - Node middleware can summarize/filter message history if needed
   - LLM formats response: "Here's a pirate joke for you: Why is a pirate's favorite letter R? ..."
   - Adds final AIMessage to history

5. **END Node**: Returns AIMessage content to caller

**Key Features Demonstrated:**
- Message history tracking (HumanMessage → AIMessage/ToolCallMessage → ToolMessage → AIMessage)
- Tool availability and invocation
- Multi-step LLM processing with tool results
- Node-level middleware for message management
- Edge routing (automatic progression through tool execution)



**Metrics Captured:**
- Total execution time: `wolf.execution.duration`
- LLM API calls: 3 (Decompose, Summarize × N, Aggregate)
- Time spent in LLM calls: `wolf.llm.api.duration` × number of calls
- Tool invocations: N web searches
- Token usage breakdown by operation

---


## Appendix A: Glossary

- **Node**: A single unit of execution in the graph
- **Edge**: A connection between two nodes defining control flow
- **Checkpoint**: A snapshot of execution state at a point in time
- **Graph**: A collection of nodes and edges defining a workflow
- **Execution Context**: Runtime state and metadata during execution
- **Span**: A unit of work in distributed tracing
- **HumanMessage**: User input or request in the conversation history
- **AIMessage**: LLM output response in the conversation history
- **ToolCallMessage**: LLM decision to invoke a tool with parameters
- **ToolMessage**: Tool execution result/response in the conversation history

---

## Appendix B: References

- OpenTelemetry Specification: https://opentelemetry.io/docs/specs/
- Directed Acyclic Graphs: https://en.wikipedia.org/wiki/Directed_acyclic_graph
- Agentic AI Patterns: LangGraph, AutoGen, Semantic Kernel

---

*Last Updated: January 28, 2026*
