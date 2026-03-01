# 09 — Tool Optimization

When the total number of available tools exceeds a configurable threshold, the tool optimizer uses a cheap LLM to select the most relevant subset for each user turn. This keeps tool counts manageable for the primary model.

## Architecture

```mermaid
classDiagram
    class ToolOptimizer {
        -TornadoApi _api
        -ChatModel _model
        -int _maxTools
        -HashSet~string~ BuiltInToolNames$
        +OptimizeAsync(allTools, userMessage) Task~ToolOptimizationResult~
        -SelectToolsAsync(candidates, budget, msg) Task~List~string~~
        -ParseSelectedTools(json) List~string~$
    }

    class ToolOptimizationResult {
        +List~Tool~ Tools
        +bool WasOptimized
        +int OriginalCount
        +int SelectedCount
        +string FallbackReason
        +Fallback(allTools, reason) ToolOptimizationResult$
    }

    ToolOptimizer --> ToolOptimizationResult
```

## When Optimization Triggers

```mermaid
flowchart TD
    Turn["New user turn"] --> Check{"ToolOptimizerEnabled<br/>AND<br/>OptimizerModel available<br/>AND<br/>TotalToolCount > MaxTools?"}
    Check -->|"No"| Skip["Skip optimization<br/>(use all tools)"]
    Check -->|"Yes"| Optimize["Run ToolOptimizer"]
```

Default threshold: **25 tools**. With built-in tools (3) + multiple skill scripts + MCP tools, this threshold can easily be exceeded.

## Optimization Flow

```mermaid
flowchart TD
    Start["OptimizeAsync(allTools, userMessage)"] --> Separate["Separate built-in tools<br/>from candidates"]
    Separate --> Budget["Budget = MaxTools - builtIn.Count"]
    Budget --> WithinBudget{"Candidates ≤ budget?"}
    WithinBudget -->|"Yes"| NoOp["Return full list<br/>WasOptimized = false"]
    WithinBudget -->|"No"| Select["SelectToolsAsync()"]
    Select --> Parse["Parse LLM response"]
    Parse --> Filter["Filter candidates<br/>to selected names"]
    Filter --> Validate{"Any matches<br/>beyond built-ins?"}
    Validate -->|"No"| Fallback["Return full list<br/>(fallback: no matches)"]
    Validate -->|"Yes"| Result["Return optimized list<br/>builtIn + selected"]
```

## LLM Selection Call

The optimizer uses **structured output** with `ToolParamListEnum` to constrain the LLM's response to only valid tool names:

```mermaid
sequenceDiagram
    participant TO as ToolOptimizer
    participant LLM as Cheap Model<br/>(e.g. Gemini 2.5 Flash)

    TO->>TO: Build tool catalog<br/>(name: description per tool)
    TO->>TO: Create ToolParamListEnum<br/>(constrains output to valid names)
    TO->>LLM: System: "You are a tool selector..."<br/>User: "Message: {userMsg}<br/>Available tools: {catalog}"<br/>ResponseFormat: StructuredJson

    LLM-->>TO: {"selected_tools": ["tool_a", "tool_b", ...]}
    TO->>TO: ParseSelectedTools(json)
```

### System Prompt

```
You are a tool selector. Given a user's message and a catalog of available tools,
select the {budget} tools most relevant to fulfilling the user's request.
Consider what operations the user might need and pick tools that would be useful.
If fewer than {budget} tools are relevant, select only the relevant ones.
```

### User Prompt

```
User message: {userMessage}

Available tools:
- filesystem__read_file: Read the contents of a file
- filesystem__write_file: Write content to a file
- web-search__search: Search the web for information
- note-taker__create: Create a new note
- ...
```

### Response Schema

```json
{
  "selected_tools": ["filesystem__read_file", "filesystem__write_file", "web-search__search"]
}
```

The `ToolParamListEnum` ensures the LLM can only output valid tool names from the provided catalog.

## Built-in Tool Protection

Three tools are **always included** regardless of optimization:

| Tool | Reason |
|------|--------|
| `load_skill` | Must always be able to activate skills |
| `list_skills` | Must always be able to discover skills |
| `read_reference` | Must always be able to read skill references |

These are separated before optimization and added back unconditionally:

```mermaid
flowchart LR
    All["40 tools total"] --> Split["Split"]
    Split --> BI["3 built-in<br/>(always kept)"]
    Split --> Candidates["37 candidates"]
    Candidates --> LLM["LLM selects 22<br/>(budget = 25 - 3)"]
    BI --> Final["25 tools<br/>(3 built-in + 22 selected)"]
    LLM --> Final
```

## Agent Tool Swapping

The `AgentBuilder` manages swapping tools on the agent for each turn:

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant AB as AgentBuilder
    participant Agent as TornadoAgent

    Note over Agent: Agent has 40 tools

    FE->>AB: OptimizeToolsForTurn("search for auth bugs")

    AB->>AB: ToolOptimizer.OptimizeAsync()
    AB->>Agent: ClearTools()
    AB->>Agent: AddTool(each of 25 optimized tools)
    AB->>Agent: Update ToolPermissionRequired

    Note over Agent: Agent has 25 tools for this turn

    FE->>FE: Process agent turn...

    FE->>AB: RestoreFullTools()
    AB->>Agent: ClearTools()
    AB->>Agent: AddTool(each of 40 full tools)
    AB->>Agent: Update ToolPermissionRequired

    Note over Agent: Agent has 40 tools again
```

## Fallback Scenarios

The optimizer degrades gracefully on any failure:

```mermaid
flowchart TD
    Optimize["OptimizeAsync()"] --> TryCatch["try/catch"]

    TryCatch -->|"Empty selection"| FB1["Fallback: 'empty selection'"]
    TryCatch -->|"No matches after filter"| FB2["Fallback: 'no matching tools'"]
    TryCatch -->|"Exception"| FB3["Fallback: exception message"]
    TryCatch -->|"Success"| OK["Return optimized list"]

    FB1 --> Full["Return full tool list<br/>WasOptimized = false"]
    FB2 --> Full
    FB3 --> Full
```

All fallback results include the `FallbackReason` for diagnostic logging.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `ToolOptimizerEnabled` | `true` | Master switch for optimization |
| `MaxTools` | `25` | Threshold that triggers optimization |

Both can be changed at runtime via `AgentBuilder`:

```csharp
builder.SetOptimizerEnabled(true, optimizerModel);
builder.SetMaxTools(30, optimizerModel);
```

## Cost Considerations

The optimizer uses the **cheapest available model** (see [08-provider-detection.md](08-provider-detection.md)):

| Priority | Model | Why |
|----------|-------|-----|
| 1 | Google Gemini 2.5 Flash | Free tier, very fast |
| 2 | OpenAI O4 Mini | Low cost per token |
| 3 | Anthropic Claude 4 Sonnet | Mid-tier pricing |
| 4 | Groq Llama 4 Scout | Free/very cheap |
| 5+ | DeepSeek, Mistral, xAI | Various |

The optimization call is lightweight — it sends only tool names + descriptions (not full schemas) plus the user message, and receives a small JSON array back.
