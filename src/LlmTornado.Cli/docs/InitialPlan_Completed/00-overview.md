# LlmTornado.Cli — Implementation Plan

An interactive CLI agent powered by ChatRuntime that uses the open [Agent Skills](https://agentskills.io) standard for context and tool discovery, MCP servers for remote tools, persistent conversation memory with LLM summarization, and a slash-command system for session management.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs (REPL)                        │
│  ┌──────────┐  ┌───────────────┐  ┌─────────────────────────┐  │
│  │ Console  │  │  Command      │  │  ChatRuntime             │  │
│  │ Renderer │  │  Dispatcher   │  │  ┌─────────────────────┐ │  │
│  │          │  │  /help        │  │  │ CliRuntimeConfig    │ │  │
│  │ Streaming│  │  /model       │  │  │                     │ │  │
│  │ Colors   │  │  /skill       │  │  │ TornadoAgent        │ │  │
│  │ Tables   │  │  /conversation│  │  │ ├─ Skill tools      │ │  │
│  │ Prompts  │  │  /tools       │  │  │ ├─ Script tools     │ │  │
│  │          │  │  /mcp         │  │  │ ├─ MCP tools        │ │  │
│  │          │  │  /clear       │  │  │ └─ load_skill tool  │ │  │
│  │          │  │  /exit        │  │  │                     │ │  │
│  └──────────┘  └───────────────┘  │  └─────────────────────┘ │  │
│                                   └─────────────────────────────┘│
│  ┌────────────┐  ┌──────────────┐  ┌─────────────────────────┐  │
│  │ Provider   │  │ Skill        │  │ Conversation Memory     │  │
│  │ Detector   │  │ Manager      │  │ Manager                 │  │
│  │            │  │              │  │                         │  │
│  │ Env vars → │  │ SKILL.md     │  │ PersistentConversation  │  │
│  │ TornadoApi │  │ scripts/     │  │ +Summarization          │  │
│  │ +Models    │  │ references/  │  │ +ConversationStore      │  │
│  └────────────┘  └──────────────┘  └─────────────────────────┘  │
│  ┌────────────┐  ┌──────────────┐  ┌─────────────────────────┐  │
│  │ MCP Config │  │ Tool         │  │ CLI Storage             │  │
│  │ Loader     │  │ Approval     │  │                         │  │
│  │            │  │ Manager      │  │ %APPDATA%\LlmTornado\   │  │
│  │ mcp.json → │  │              │  │ ├─ conversations/       │  │
│  │ MCPServer  │  │ First-use    │  │ ├─ tool-approvals.json  │  │
│  │ instances  │  │ Always allow │  │ └─ settings.json        │  │
│  └────────────┘  └──────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Stages

| # | Stage | File(s) to Create | Doc |
|---|-------|-------------------|-----|
| 1 | [Project Scaffolding](01-project-scaffolding.md) | `.csproj`, solution ref | Setup the executable project and wire dependencies |
| 2 | [Provider Detection](02-provider-detection.md) | `ProviderDetector.cs` | Auto-detect LLM providers from environment variables |
| 3 | [Storage Layout](03-storage-layout.md) | `CliStorage.cs`, `CliSettings.cs` | Persistent data directory with settings, approvals, conversations |
| 4 | [Skill System](04-skill-system.md) | `Skills/*.cs` | Agent Skills standard: discovery, parsing, script tools |
| 5 | [MCP Configuration](05-mcp-configuration.md) | `Mcp/McpConfigLoader.cs` | Load MCP servers from JSON, initialize tools |
| 6 | [Tool Approval](06-tool-approval.md) | `ToolApprovalManager.cs` | First-use approval with "always allow" persistence |
| 7 | [Conversation Memory](07-conversation-memory.md) | `Memory/*.cs` | Persistent history, LLM summarization, save/load |
| 8 | [Agent Builder](08-agent-builder.md) | `CliAgentBuilder.cs` | Assemble TornadoAgent with skills, tools, MCP |
| 9 | [Slash Commands](09-slash-commands.md) | `Commands/*.cs` | `/help`, `/model`, `/skill`, `/conversation`, `/tools`, `/mcp` |
| 10 | [REPL Loop](10-repl-loop.md) | `Program.cs` | Main entry point, startup sequence, input loop |
| 11 | [Console Rendering](11-console-rendering.md) | `ConsoleRenderer.cs` | Streaming output, colors, tables, prompts |

---

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Skill format | [Agent Skills standard](https://agentskills.io/specification) (SKILL.md) | Open ecosystem standard adopted by GitHub, VS Code, Anthropic, etc. |
| Skill activation | Progressive disclosure via `load_skill` tool | Agent loads full instructions on demand, keeping context small |
| Script execution | Process spawn per script file | Matches Agent Skills standard (`scripts/` directory convention) |
| Runtime config | `SingletonRuntimeConfiguration` | Single-agent conversational loop; orchestration is overkill here |
| MCP config format | `AcpMcpServerConfig` JSON schema | Already exists in codebase, supports stdio + http |
| Context management | Adapted `ContextWindowMessageSummarizer` | Battle-tested in ContextController sample with tunable thresholds |
| Persistence format | JSONL via `PersistentConversation` | Already exists in codebase, supports append-mode crash resilience |
| Tool approval | All tools require first-use approval | Matches Agent Skills security recommendation; whitelist persists |
| Provider detection | Standard env var names per provider | Zero-config setup for users who already have provider keys |

---

## Dependency Chain

```
LlmTornado (core)
  └─ LlmTornado.Agents (ChatRuntime, TornadoAgent, PersistentConversation)
      └─ LlmTornado.Mcp (MCPServer, tool conversion)
          └─ LlmTornado.Cli (this project - Exe)
```

---

## File Layout (Final)

```
src/LlmTornado.Cli/
├── LlmTornado.Cli.csproj
├── Program.cs                          # Entry point + REPL loop
├── CliAgentBuilder.cs                  # Assembles TornadoAgent + runtime config
├── CliStorage.cs                       # Persistent data directory management
├── CliSettings.cs                      # Serializable settings model
├── ConsoleRenderer.cs                  # Output formatting, colors, streaming
├── ProviderDetector.cs                 # Env var scanning, TornadoApi construction
├── ToolApprovalManager.cs              # First-use approval + whitelist persistence
├── Commands/
│   ├── ICliCommand.cs                  # Command interface
│   ├── CommandDispatcher.cs            # Parse + dispatch /commands
│   ├── HelpCommand.cs
│   ├── ModelCommand.cs
│   ├── SkillCommand.cs
│   ├── ConversationCommand.cs
│   ├── ToolsCommand.cs
│   ├── McpCommand.cs
│   ├── ClearCommand.cs
│   └── ExitCommand.cs
├── Skills/
│   ├── CliSkill.cs                     # Skill model (name, description, scripts, etc.)
│   ├── CliSkillLoader.cs               # SKILL.md parser following Agent Skills standard
│   ├── CliSkillManager.cs              # Enable/disable, list, activate skills
│   └── ScriptToolBuilder.cs            # Convert scripts/ to Tool objects
├── Mcp/
│   ├── McpConfigLoader.cs              # mcp.json parser → MCPServer instances
│   └── McpConfigModel.cs               # JSON model for mcp.json
├── Memory/
│   ├── ConversationMemoryManager.cs    # PersistentConversation + summarization
│   ├── ConversationStore.cs            # Save/load/list/delete conversations
│   ├── MessageSummarizer.cs            # LLM-based message compression
│   └── CompressionStrategy.cs          # Threshold-based compression decisions
└── docs/
    ├── 00-overview.md                  # This file
    ├── 01-project-scaffolding.md
    ├── 02-provider-detection.md
    ├── 03-storage-layout.md
    ├── 04-skill-system.md
    ├── 05-mcp-configuration.md
    ├── 06-tool-approval.md
    ├── 07-conversation-memory.md
    ├── 08-agent-builder.md
    ├── 09-slash-commands.md
    ├── 10-repl-loop.md
    └── 11-console-rendering.md
```
