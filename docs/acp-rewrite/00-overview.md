# ACP Server Rewrite — Overview

## Goal

Rewrite the ACP server (`LlmTornado.Acp.Server`) to use the CLI's agent infrastructure instead of its own separate, simpler skill system. The CLI (`LlmTornado.Cli`) has a battle-tested agent/skill/MCP pipeline that works well in practice — the ACP server should be a thin adapter that exposes the same agents over the ACP (Agent Client Protocol) JSON-RPC transport for IDE integration.

## Current State

### CLI (`LlmTornado.Cli`) — Works Well
- **5 built-in agent personas**: `default`, `architect`, `code-reviewer`, `debugger`, `docs-writer`
- **12-provider detection** from environment variables (OpenAI, Anthropic, Google, etc.)
- **Skills system**: SKILL.md directories with scripts, references, progressive disclosure
- **MCP integration**: Loads MCP servers from `mcp.json` config
- **Tool optimizer**: LLM-based per-turn tool selection when tool count exceeds threshold
- **Capability curation**: Personas whitelist/blacklist skills and tools
- **Agent builder**: Assembles TornadoAgent + ChatRuntime from all subsystems

### ACP Server (`LlmTornado.Acp.Server`) — Needs Rewrite
- **OpenAI-only** — hardcoded `OPENAI_API_KEY`
- **4 hardcoded modes**: agent, chat, plan, refactor (as embedded C# string literals)
- **No skills system** — only filesystem tools (list_dir, read_file, etc.) 
- **No MCP support**
- **Orchestrated refactor pipeline** — over-engineered multi-stage pipeline that underperforms
- **Duplicate code** — reimplements YAML parsing, skill loading, etc. separately

## Architecture After Rewrite

```
┌──────────────────────────────────────────────────────────┐
│                  LlmTornado.Cli.Core                      │
│  (shared library — new project)                           │
│                                                           │
│  ┌───────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ AgentDefinition│  │ SkillManager │  │ProviderDetector│  │
│  │ + Loader       │  │ + Loader     │  │               │  │
│  └───────────────┘  └──────────────┘  └───────────────┘  │
│  ┌───────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ McpConfigLoader│  │ AgentBuilder │  │ ToolOptimizer │  │
│  │ + Models       │  │ (shared)     │  │               │  │
│  └───────────────┘  └──────────────┘  └───────────────┘  │
│  ┌───────────────┐  ┌──────────────┐                     │
│  │ AgentSettings  │  │ISettings...  │  Built-in .md files │
│  │ (data model)   │  │(persistence) │                     │
│  └───────────────┘  └──────────────┘                     │
└──────────────────────────────────────────────────────────┘
            ▲                               ▲
            │                               │
   ┌────────┴────────┐            ┌────────┴────────┐
   │  LlmTornado.Cli  │            │ LlmTornado.Acp  │
   │  (console app)    │            │ .Server          │
   │                   │            │ (JSON-RPC server)│
   │ ConsoleRenderer   │            │                  │
   │ ToolApprovalMgr   │            │ TornadoAcpRuntime│
   │ ConversationMemory│            │ Filesystem tools │
   │ REPL loop         │            │ ACP protocol     │
   │                   │            │ adapter           │
   └───────────────────┘            └──────────────────┘
```

## Mapping: CLI Agents → ACP Modes

| CLI Persona (`built-in/*.md`) | ACP Mode ID | ACP Mode Display Name | Description |
|------|------|------|------|
| `default.md` | `default` | Default | General-purpose assistant with all skills and tools |
| `architect.md` | `architect` | Architect | Software architecture, design, trade-offs |
| `code-reviewer.md` | `code-reviewer` | Code Reviewer | Security/quality focused code review |
| `debugger.md` | `debugger` | Debugger | Hypothesis-driven systematic debugging |
| `docs-writer.md` | `docs-writer` | Docs Writer | Technical documentation writing |

Custom agents from `ACP_AGENTS_DIR` also become ACP modes automatically.

## Phases

| Phase | Description | Difficulty | Files Touched |
|-------|-------------|------------|---------------|
| [Phase 1](01-phase-create-core-project.md) | Create `LlmTornado.Cli.Core` project | Easy | New project, solution file |
| [Phase 2](02-phase-extract-data-models.md) | Extract data models + persistence abstraction | Easy | Move & rename types |
| [Phase 3](03-phase-extract-loaders.md) | Extract loaders (agent, skill, MCP) | Easy | Move & parameterize |
| [Phase 4](04-phase-extract-managers.md) | Extract managers + provider detection | Medium | Refactor DI, move types |
| [Phase 5](05-phase-shared-agent-builder.md) | Create shared AgentBuilder | Medium | Extract from CliAgentBuilder |
| [Phase 6](06-phase-update-cli.md) | Update CLI to use Core | Medium | Replace internals with refs |
| [Phase 7](07-phase-rewrite-acp-server.md) | Rewrite ACP Server | Hard | Full rewrite of server |
| [Phase 8](08-phase-cleanup-verify.md) | Cleanup + verification | Easy | Delete old files, build, test |

## Key Design Decisions

1. **Shared library named `LlmTornado.Cli.Core`** — not a generic name; it's specifically the CLI agent infrastructure
2. **Agent persona `.md` files as shared content** in Core — single source of truth
3. **`ISettingsPersistence` abstraction** — CLI persists to disk, ACP server uses in-memory no-op
4. **`IToolApproval` abstraction** — CLI prompts interactively, ACP server auto-approves
5. **Filesystem tools stay in ACP server** — they're ACP-specific, scoped to IDE workspace
6. **Orchestrated refactor pipeline dropped** — all modes use single-agent architecture like CLI
7. **Full multi-provider support** via `ProviderDetector` — not restricted to OpenAI
