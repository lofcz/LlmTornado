# 03 — Agent Persona System

The agent persona system lets users switch between specialized agent behaviors (e.g., "architect", "debugger") while maintaining per-persona control over which skills and tools are available.

## Architecture

```mermaid
classDiagram
    class AgentDefinition {
        +string Name
        +string Description
        +AgentSource Source
        +string FilePath
        +string Instructions
        +List~string~ EnabledSkills
        +List~string~ DisabledSkills
        +List~string~ EnabledTools
        +List~string~ DisabledTools
        +List~string~ AutoApproveTools
        +bool HasCapabilityCuration
        +bool IsPersona
    }

    class AgentDefinitionLoader {
        +DiscoverPersonaAgents() List~AgentDefinition~
        +DiscoverProjectAgents(startDir) AgentDefinition
        +ParsePersonaFile(path, source) AgentDefinition
        +ResolveBuiltInDirectory() string
        +ResolveGlobalAgentsDirectory() string
        +ResolveAgentsDirectory(override) string
    }

    class AgentDefinitionManager {
        -Dictionary _personas
        -AgentDefinition _projectContext
        -string _activePersonaName
        -HashSet _blockedTools
        -HashSet _allowedToolsWhitelist
        -bool _hasToolWhitelist
        +LoadAll(builtIn, global, custom, cwd)
        +SetActivePersona(name) AgentDefinition
        +ClearActivePersona()
        +ApplyCapabilityBaseline(sm, ta)
        +IsToolAllowed(toolName) bool
        +BuildInstructionsBlock() string
        +RefreshProjectContext(cwd)
    }

    class AgentSource {
        <<enumeration>>
        BuiltIn
        Global
        Custom
        Project
    }

    AgentDefinitionManager --> AgentDefinition
    AgentDefinitionManager --> AgentDefinitionLoader
    AgentDefinition --> AgentSource
```

## Agent Sources & Discovery

Persona agents are discovered from three filesystem locations. Each layer shadows the previous — if a persona with the same name exists in multiple locations, the most specific one wins.

```mermaid
flowchart TD
    subgraph "Discovery Locations (precedence order)"
        BI["1. Built-in<br/><code>AppContext.BaseDirectory/Agents/built-in/</code>"]
        GL["2. Global<br/><code>TORNADO_AGENTS_DIR</code> env var<br/>or <code>%APPDATA%/llmtornado/agents/</code>"]
        CU["3. Custom / Project-local<br/>Override directory or <code>./agents/</code>"]
    end

    BI -->|"Shadowed by"| GL
    GL -->|"Shadowed by"| CU

    CU --> Result["Final persona dictionary<br/>(most specific wins)"]

    PJ["Project Context<br/>Walk up to 20 parent dirs<br/>collecting AGENTS.md files"] --> PC["Merged project context<br/>(nearest-first)"]
```

### Directory Resolution

| Source | Resolution Logic |
|--------|-----------------|
| **Built-in** | `AppContext.BaseDirectory/Agents/built-in/` |
| **Global** | `TORNADO_AGENTS_DIR` env var → `%APPDATA%/llmtornado/agents/` |
| **Custom** | Settings override → `./agents/` |
| **Project** | Walk up from CWD, collect all `AGENTS.md`, merge nearest-first |

## Persona File Format

Each persona is a Markdown file with YAML frontmatter. The filename must be a valid slug (1–64 chars, lowercase alphanumeric + hyphens, no consecutive hyphens).

```markdown
---
name: architect
description: Software architecture and design trade-offs
enabled-skills: file-analyzer web-search
auto-approve-tools: file-analyzer:tree-summary file-analyzer:line-count
---

You are an expert software architect. Focus on:
- System design and architecture patterns
- Trade-off analysis between approaches
- Scalability and maintainability considerations

Always consider the project's existing patterns before suggesting changes.
```

### Frontmatter Fields

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Persona identifier (must match filename) |
| `description` | string | Short description shown in persona list |
| `enabled-skills` | space-delimited | Whitelist — only these skills are enabled |
| `disabled-skills` | space-delimited | Blacklist — these skills are disabled |
| `enabled-tools` | space-delimited | Whitelist — only these tools are available |
| `disabled-tools` | space-delimited | Blacklist — these tools are hidden |
| `auto-approve-tools` | space-delimited | Tools that skip the approval prompt |

### Validation Rules

- Filename: 1–64 chars, lowercase `[a-z0-9-]`, no consecutive hyphens, no leading/trailing hyphens
- Max file size: 100KB
- `name` must match the filename slug
- `description` max: 1024 chars
- Frontmatter parsed as simple `key: value` pairs with nested map support

## Built-in Personas

```mermaid
graph LR
    subgraph "Built-in Personas"
        D["default<br/>All skills, all tools"]
        A["architect<br/>Design & trade-offs"]
        CR["code-reviewer<br/>Security & quality"]
        DB["debugger<br/>Hypothesis-driven"]
        DW["docs-writer<br/>Technical docs"]
        PL["planner<br/>Implementation plans"]
    end
```

| Persona | Enabled Skills | Auto-Approved Tools | Special |
|---------|----------------|---------------------|---------|
| `default` | All | None | No capability curation |
| `architect` | `file-analyzer`, `web-search` | `file-analyzer:tree-summary`, `file-analyzer:line-count` | — |
| `code-reviewer` | `file-analyzer` | `file-analyzer:*` | Disables note-taker tools |
| `debugger` | `file-analyzer`, `web-search` | `file-analyzer:*` | — |
| `docs-writer` | `file-analyzer`, `note-taker` | `file-analyzer:*`, `note-taker:list`, `note-taker:search` | — |
| `planner` | `file-analyzer`, `web-search`, `note-taker` | Analysis + notes tools | Never writes code |

## Capability Baseline Application

When a persona is activated, `ApplyCapabilityBaseline()` executes a 5-step process:

```mermaid
flowchart TD
    Start["ApplyCapabilityBaseline()"] --> S1["Step 1: Reset tool filtering state<br/>(clear blockedTools, allowedTools, hasWhitelist)"]
    S1 --> S2["Step 2: Reset ALL skills to enabled<br/>(clean slate)"]
    S2 --> Check{"Persona has<br/>capability curation?"}
    Check -->|"No"| Done["Done — all capabilities available"]
    Check -->|"Yes"| S3["Step 3: Apply skill whitelist<br/>(disable skills not in whitelist)"]
    S3 --> S4["Step 4: Apply skill blacklist<br/>(disable explicitly blocked skills)"]
    S4 --> S5["Step 5: Compute tool filtering state<br/>(populate blockedTools/allowedTools)"]
    S5 --> S6["Step 6: Pre-approve auto-approve-tools"]
    S6 --> Done2["Done — persona capabilities applied"]
```

## Tool Filtering Logic

After the capability baseline is applied, `IsToolAllowed()` checks each tool:

```mermaid
flowchart TD
    Check["IsToolAllowed(toolName)"] --> BuiltIn{"Built-in tool?<br/>(load_skill, list_skills,<br/>read_reference)"}
    BuiltIn -->|"Yes"| Allow["✓ Always allowed"]
    BuiltIn -->|"No"| Active{"Active persona<br/>set?"}
    Active -->|"No"| Allow
    Active -->|"Yes"| HasCuration{"Has whitelist<br/>or blacklist?"}
    HasCuration -->|"No"| Allow
    HasCuration -->|"Yes"| WL{"Has tool<br/>whitelist?"}
    WL -->|"Yes"| InWL{"Tool in<br/>whitelist?"}
    InWL -->|"No"| Block["✗ Blocked"]
    InWL -->|"Yes"| BL{"Tool in<br/>blacklist?"}
    WL -->|"No"| BL
    BL -->|"Yes"| Block
    BL -->|"No"| Allow
```

## Project Context (AGENTS.md)

Project context is separate from personas. It provides project-specific instructions that are always included in the system prompt (regardless of which persona is active).

```mermaid
flowchart TD
    CWD["Current Working Directory"] --> Walk["Walk up to 20 parent<br/>directories"]
    Walk --> Collect["Collect all AGENTS.md files"]
    Collect --> Merge["Merge nearest-first<br/>(closest directory's content first)"]
    Merge --> Result["Single AgentDefinition<br/>Source = Project"]
    Result --> SP["Injected as<br/>&lt;project_context&gt; block<br/>in system prompt"]
```

The `ProjectAgentsEnabled` setting (default: `true`) controls whether AGENTS.md scanning is active.

## Instructions Block Assembly

`BuildInstructionsBlock()` combines persona + project context into XML blocks for the system prompt:

```xml
<agent_persona>
(Active persona's markdown instructions)
</agent_persona>

<project_context>
(Merged AGENTS.md content from parent directories)
</project_context>
```

If no persona is active, only the project context block is included (if found). If neither exists, the `AgentBuilder` falls back to a generic "You are a helpful assistant" prompt.
