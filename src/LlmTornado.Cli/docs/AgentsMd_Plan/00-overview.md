# AGENTS.md Integration — Implementation Plan

An extension to the CLI agent that integrates the open [AGENTS.md](https://agents.md) format for two complementary purposes: **project context** (auto-detected repository instructions) and **selectable agent personas** (curated behavior profiles with skill/tool curation). This builds on top of the existing Skill system, MCP integration, and Tool Approval infrastructure.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Program.cs (REPL)                                    │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                     CliAgentBuilder.Build()                          │   │
│  │                                                                      │   │
│  │  System Prompt Assembly Order:                                       │   │
│  │  ┌─────────────────────────────────────────────────────────┐        │   │
│  │  │ 1. Active Persona Instructions (from agent .md file)    │        │   │
│  │  ├─────────────────────────────────────────────────────────┤        │   │
│  │  │ 2. Project AGENTS.md Context (from CWD hierarchy)       │        │   │
│  │  ├─────────────────────────────────────────────────────────┤        │   │
│  │  │ 3. <available_skills> XML (from CliSkillManager)        │        │   │
│  │  ├─────────────────────────────────────────────────────────┤        │   │
│  │  │ 4. Working Directory Info                               │        │   │
│  │  └─────────────────────────────────────────────────────────┘        │   │
│  │                                                                      │   │
│  │  Tool Collection (filtered by active persona):                       │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                │   │
│  │  │ Built-in     │ │ Script tools │ │ MCP tools    │                │   │
│  │  │ load_skill   │ │ skill:script │ │ server:tool  │                │   │
│  │  │ list_skills  │ │ (filtered)   │ │ (filtered)   │                │   │
│  │  │ read_ref     │ │              │ │              │                │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌───────────────────┐  ┌──────────────────────────────────────────────┐   │
│  │ AgentDefinition   │  │ Capability Curation                          │   │
│  │ Manager           │  │                                              │   │
│  │                   │  │ Active Persona sets baseline:                │   │
│  │ ┌───────────────┐ │  │  enabled-skills: file-analyzer              │   │
│  │ │ Project       │ │  │  disabled-skills: note-taker                │   │
│  │ │ AGENTS.md     │ │  │  disabled-tools: web-search:fetch-url      │   │
│  │ │ (CWD → root)  │ │  │  auto-approve-tools: file-analyzer:*       │   │
│  │ ├───────────────┤ │  │                                              │   │
│  │ │ Persona       │ │  │ User can override per-session:              │   │
│  │ │ Agents        │ │  │  /skill enable web-search                   │   │
│  │ │ (built-in +   │ │  │  /skill disable file-analyzer               │   │
│  │ │  custom)      │ │  │  (resets on next /agent set)                │   │
│  │ └───────────────┘ │  └──────────────────────────────────────────────┘   │
│  └───────────────────┘                                                     │
│                                                                             │
│  ┌───────────────────┐  ┌──────────────────────────────────────────────┐   │
│  │ /agent command    │  │ Existing Infrastructure (unchanged)          │   │
│  │                   │  │                                              │   │
│  │ /agent            │  │ CliSkillManager    (skills baseline target)  │   │
│  │ /agent list       │  │ ToolApprovalManager (auto-approve target)   │   │
│  │ /agent set <name> │  │ McpConfigLoader    (tools filtered)         │   │
│  │ /agent clear      │  │ /skill command     (session overrides)      │   │
│  │ /agent info <n>   │  │ /tools command     (shows blocked tools)    │   │
│  │ /agent project    │  │                                              │   │
│  └───────────────────┘  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Two-Layer Design

### Layer 1: Project Context (AGENTS.md Auto-Detection)

Per the [AGENTS.md specification](https://agents.md), `AGENTS.md` files are plain markdown placed in a repository to guide coding agents. They contain no frontmatter — just headings and instructions covering build steps, conventions, testing commands, and project-specific context.

The CLI walks from the current working directory toward the filesystem root, collecting every `AGENTS.md` file found. These are merged (nearest-first) into a single block injected into the system prompt. This gives the agent repository-aware context automatically.

**Key property**: Project AGENTS.md files provide **context only** — they do not curate skills or tools. This matches the spec's intent: AGENTS.md is instructions for the agent, not configuration of the agent.

### Layer 2: Selectable Agent Personas

Agent personas are our own extension: curated `.md` files that define behavioral profiles with optional skill/tool curation via YAML frontmatter. They live in an `agents/` directory (built-in + custom).

Each persona defines:
- **Instructions** (markdown body): Behavioral guidelines, workflow preferences, response style
- **Capability curation** (YAML frontmatter): Which skills/tools are enabled, disabled, or auto-approved

Only one persona is active at a time. Switching agents resets the capability baseline. The user can then override per-session via `/skill` and `/tools` commands.

---

## Implementation Phases

| # | Phase | Files to Create/Modify | Doc |
|---|-------|------------------------|-----|
| 1 | [Data Model & Loader](01-model-and-loader.md) | `Agents/CliAgentDefinition.cs`, `Agents/AgentDefinitionLoader.cs` | Agent definition model, AGENTS.md parsing, CWD hierarchy walker, persona file parser |
| 2 | [Manager & Settings](02-manager-and-settings.md) | `Agents/AgentDefinitionManager.cs`, modify `CliSettings.cs` | Persona selection lifecycle, capability baseline application, settings persistence |
| 3 | [Agent Builder Integration](03-builder-integration.md) | Modify `CliAgentBuilder.cs` | System prompt layering, tool filtering, rebuild methods |
| 4 | [Built-in Agent Personas](04-built-in-agents.md) | `Agents/built-in/*.md`, modify `.csproj` | Shipped agent definitions with curated capability profiles |
| 5 | [Command & Program Wiring](05-command-and-program.md) | `Commands/AgentCommand.cs`, modify `Program.cs`, modify `SkillCommand.cs`, modify `ToolsCommand.cs` | `/agent` command, initialization step, existing command updates |
| 6 | [Tests](06-tests.md) | `LlmTornado.Cli.Tests/AgentDefinitionTests.cs` | Comprehensive test coverage for all new functionality |

---

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Project AGENTS.md format | Pure markdown (no frontmatter) | Follows the open AGENTS.md spec exactly — interoperable with Copilot, Codex, Windsurf, etc. |
| Persona file format | YAML frontmatter + markdown body | Consistent with existing SKILL.md pattern in the codebase; reuses `ParseFrontmatter` logic |
| Persona selection | Exclusive (one at a time) | Cleaner system prompt, predictable behavior, simpler mental model |
| Capability model | Whitelist + blacklist | `enabled-skills` (if non-empty) = whitelist; `disabled-skills` = blacklist. Covers both "only these" and "everything except" patterns |
| Override model | Agent sets baseline, user overrides per-session | Predictable: `/agent set X` always produces the same starting state. User retains control. |
| AGENTS.md hierarchy | Walk CWD → root, nearest-first merge | Matches spec: "the closest one takes precedence" for monorepo support |
| Built-in agent storage | `<Content CopyToOutputDirectory>` | Keeps agents as editable/inspectable files; consistent with skill filesystem model |
| Project AGENTS.md capability curation | None (context only) | Project AGENTS.md is an open standard for instructions, not a configuration format |

---

## Interaction Matrix

Shows how the new agent system interacts with existing CLI components:

| Component | Interaction |
|-----------|-------------|
| **CliSkillManager** | Agent persona calls `EnableSkill`/`DisableSkill` to set baseline. User can still override. |
| **ToolApprovalManager** | Agent persona calls `PreApproveSkillTools` for `auto-approve-tools`. Persona's `disabled-tools` filtered in `CollectTools()`. |
| **McpConfigLoader** | MCP tools are subject to persona's `enabled-tools`/`disabled-tools` filtering. |
| **ConversationMemoryManager** | Conversation history preserved across agent switches. Only system prompt changes. |
| **CommandDispatcher** | New `/agent` command registered alongside existing commands. |
| **ConsoleRenderer** | Agent name shown in prompt: `[code-reviewer] gpt-4o >` |
| **CliStorage** | `settings.json` extended with `active_agent`, `agents_directory`, `project_agents_enabled` |

---

## File Layout (After Implementation)

```
src/LlmTornado.Cli/
├── ... (existing files unchanged)
├── Agents/                              # NEW DIRECTORY
│   ├── CliAgentDefinition.cs            # Data model for agent personas + project context
│   ├── AgentDefinitionLoader.cs         # Discovery, parsing, hierarchy walking
│   ├── AgentDefinitionManager.cs        # Lifecycle: select, clear, baseline application
│   └── built-in/                        # Shipped agent persona files
│       ├── code-reviewer.md
│       ├── debugger.md
│       ├── docs-writer.md
│       ├── architect.md
│       └── default.md
├── Commands/
│   ├── AgentCommand.cs                  # NEW: /agent [list|set|clear|info|project]
│   ├── SkillCommand.cs                  # MODIFIED: shows agent-imposed state
│   └── ToolsCommand.cs                  # MODIFIED: shows agent-blocked tools
├── CliAgentBuilder.cs                   # MODIFIED: agent context in prompt, tool filtering
├── CliSettings.cs                       # MODIFIED: +active_agent, +agents_directory, +project_agents_enabled
└── LlmTornado.Cli.csproj               # MODIFIED: Content includes for built-in agents
```

---

## Dependency Chain

No new project dependencies. The agent system is built entirely within `LlmTornado.Cli` using existing infrastructure:

```
LlmTornado (core)
  └─ LlmTornado.Agents (ChatRuntime, TornadoAgent)
      └─ LlmTornado.Mcp (MCPServer)
          └─ LlmTornado.Cli
              ├─ Skills/ (existing — baseline target)
              ├─ Agents/ (NEW — persona management)
              ├─ Commands/ (extended)
              └─ CliAgentBuilder.cs (extended)
```
