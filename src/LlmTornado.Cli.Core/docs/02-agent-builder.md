# 02 — AgentBuilder: Central Orchestrator

The `AgentBuilder` is the single entry point shared between the CLI and ACP Server. It assembles a `TornadoAgent` and `ChatRuntime` from all system components.

## Class Overview

```mermaid
classDiagram
    class AgentBuilder {
        -TornadoApi _api
        -SkillManager _skillManager
        -McpConfigLoader _mcpLoader
        -IToolApproval _toolApproval
        -AgentDefinitionManager _agentManager
        -AgentSettings _settings
        -List~Tool~ _additionalTools
        -ChatModel _activeModel
        -TornadoAgent _agent
        -ChatRuntime _runtime
        -ToolOptimizer _toolOptimizer
        -List~Tool~ _fullToolList
        +string WorkingDirectory
        +bool NeedsOptimization
        +int TotalToolCount
        +Build(onRuntimeEvent) ChatRuntime
        +SetModel(model) ChatRuntime
        +RebuildForSkillChange() ChatRuntime
        +RebuildForAgentChange() ChatRuntime
        +OptimizeToolsForTurn(userMessage) Task~ToolOptimizationResult~
        +RestoreFullTools() void
        +SetOptimizerEnabled(bool) void
        +SetMaxTools(int) void
    }

    AgentBuilder --> TornadoAgent
    AgentBuilder --> ChatRuntime
    AgentBuilder --> SkillManager
    AgentBuilder --> McpConfigLoader
    AgentBuilder --> AgentDefinitionManager
    AgentBuilder --> ToolOptimizer
    AgentBuilder --> IToolApproval
```

## Build Process

The `Build()` method is the core operation. It can be called multiple times (e.g., after model change, skill toggle, or persona switch).

```mermaid
flowchart TD
    Start["Build()"] --> SP["BuildSystemPrompt()"]
    SP --> CT["CollectTools()"]
    CT --> Agent["Create TornadoAgent<br/>(streaming, with model)"]
    Agent --> AddTools["Add all tools to agent"]
    AddTools --> Perms["Register tool permissions<br/>(all tools require approval)"]
    Perms --> PreApprove["Pre-approve skill allowed-tools"]
    PreApprove --> RT["Create ChatRuntime<br/>(SingletonRuntimeConfiguration)"]
    RT --> Return["Return ChatRuntime"]
```

## System Prompt Assembly

The system prompt is built in layers:

```mermaid
flowchart LR
    subgraph "Layer 1: Agent Instructions"
        P["&lt;agent_persona&gt;<br/>Persona markdown"]
        PC["&lt;project_context&gt;<br/>AGENTS.md content"]
    end

    subgraph "Layer 2: Skills Catalog"
        SC["&lt;available_skills&gt;<br/>XML skill metadata"]
    end

    subgraph "Layer 3: Environment"
        CWD["Current working directory"]
    end

    P --> Final["Final System Prompt"]
    PC --> Final
    SC --> Final
    CWD --> Final
```

### Example System Prompt Structure

```xml
<agent_persona>
You are an expert software architect...
(persona markdown from selected .md file)
</agent_persona>

<project_context>
This project uses .NET 8 and follows clean architecture...
(merged content from AGENTS.md files found in parent directories)
</project_context>

<available_skills>
  <skill>
    <name>file-analyzer</name>
    <description>Analyze file structure and content</description>
    <location>/path/to/skills/file-analyzer/SKILL.md</location>
    <scripts>tree-summary.py, line-count.sh</scripts>
  </skill>
</available_skills>

When a user's task matches a skill, use the `load_skill` tool to activate it.
The skill's full instructions will be returned and you should follow them.

The user's current working directory is: C:\Users\john\project
```

## Tool Collection

`CollectTools()` aggregates tools from all sources, then filters by the active persona's curation rules.

```mermaid
flowchart TD
    subgraph "Built-in Tools"
        LS["load_skill<br/>(activate a skill)"]
        LSK["list_skills<br/>(list enabled skills)"]
        RR["read_reference<br/>(read skill reference file)"]
    end

    subgraph "Skill Script Tools"
        SS["Enabled skills' scripts<br/>(approval-gated)"]
    end

    subgraph "MCP Tools"
        MCP["Tools from MCP servers<br/>(stdio / HTTP)"]
    end

    subgraph "Additional Tools"
        AT["Front-end provided tools<br/>(e.g. ACP filesystem tools)"]
    end

    LS --> Merge["Merge all tools"]
    LSK --> Merge
    RR --> Merge
    SS --> Merge
    MCP --> Merge
    AT --> Merge
    Merge --> Filter{"Persona tool<br/>curation filter"}
    Filter -->|"Allowed"| Final["Final tool list"]
    Filter -->|"Blocked"| X["Excluded"]
```

**Important**: The three built-in tools (`load_skill`, `list_skills`, `read_reference`) always pass the persona filter, regardless of whitelist/blacklist configuration.

## Built-in Tools

| Tool | Parameters | Description |
|------|-----------|-------------|
| `load_skill` | `skillName: string` | Loads a skill's full instructions on demand (progressive disclosure). Returns instructions text or error. |
| `list_skills` | *(none)* | Lists all enabled skills with descriptions, activation status, and available scripts. |
| `read_reference` | `skillName: string, relativePath: string` | Reads a file from a skill's directory. Path-traversal protected (must stay within skill dir). Output capped at 30KB. |

## Rebuild Scenarios

The agent can be rebuilt at runtime for several reasons:

```mermaid
flowchart LR
    MC["Model Change"] -->|"SetModel()"| Build["Build()"]
    SK["Skill Enable/Disable"] -->|"RebuildForSkillChange()"| Build
    AP["Persona Switch"] -->|"RebuildForAgentChange()"| Build2["ApplyCapabilityBaseline()<br/>→ Build()"]
```

`RebuildForAgentChange()` is special — it first applies the new persona's capability baseline (resetting all skills, then applying the persona's whitelist/blacklist) before rebuilding.

## Tool Optimization Flow (Per-Turn)

When the total tool count exceeds the configured threshold (default: 25), the optimizer runs on each user turn:

```mermaid
sequenceDiagram
    participant Frontend
    participant AB as AgentBuilder
    participant TO as ToolOptimizer
    participant Agent as TornadoAgent

    Frontend->>AB: OptimizeToolsForTurn(userMessage)
    AB->>TO: OptimizeAsync(fullToolList, userMessage)
    TO-->>AB: ToolOptimizationResult{optimized subset}
    AB->>Agent: ClearTools() + AddTool(each optimized tool)
    AB-->>Frontend: result

    Note over Frontend: Agent processes the turn...

    Frontend->>AB: RestoreFullTools()
    AB->>Agent: ClearTools() + AddTool(each full tool)
```

## Configuration Properties

The `AgentBuilder` respects these `AgentSettings` values:

| Setting | Default | Effect |
|---------|---------|--------|
| `ToolOptimizerEnabled` | `true` | Whether per-turn tool optimization is active |
| `MaxTools` | `25` | Tool count threshold that triggers optimization |
| `DisabledSkills` | `[]` | Skills excluded from tool collection |
| `ActiveAgent` | `null` | Persona name that controls capability curation |
