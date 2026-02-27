# LlmTornado.Acp.Server

A ready-to-run ACP (Agent Client Protocol) server that provides skill-based AI coding assistant modes for JetBrains Rider and other ACP-compatible editors. Built on top of `LlmTornado.Acp` and the LlmTornado agent orchestration framework.

## Overview

Acp.Server is a stdio-based JSON-RPC 2.0 server that connects AI models to your IDE through the ACP protocol. It implements a **skill-based architecture** where each assistant mode (agent, chat, plan, refactor) is driven by a self-contained skill definition loaded from a `SKILL.md` file.

```
IDE (JetBrains Rider)          Acp.Server                     OpenAI
       │                            │                            │
       │── initialize ─────────────►│                            │
       │◄── capabilities ───────────│                            │
       │                            │                            │
       │── session/new ────────────►│  (loads skill for mode)    │
       │◄── sessionId, modes ───────│                            │
       │                            │                            │
       │── session/prompt ─────────►│── chat completion ────────►│
       │◄── session/update (stream) │◄── streaming tokens ───────│
       │◄── promptResponse ─────────│                            │
```

## Quick Start

### 1. Set Your API Key

```bash
# Option A: Environment variable
export OPENAI_API_KEY=sk-...

# Option B: apiKey.json file (next to the executable or in LlmTornado.Demo/)
echo '{"OpenAi": "sk-..."}' > apiKey.json
```

### 2. Run the Server

```bash
cd src/LlmTornado.Acp.Server
dotnet run
```

The server listens on stdin/stdout for JSON-RPC messages. Diagnostic output goes to stderr.

### 3. Configure Your Editor

#### JetBrains Rider

Add to your ACP agent configuration:

```json
{
  "acpAgents": {
    "tornado": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/LlmTornado.Acp.Server"]
    }
  }
}
```

## Architecture

### Skill-Based Design

Each assistant mode is powered by an **AgentSkill** — a self-contained definition that specifies:
- The agent's system prompt (instructions)
- Whether filesystem tools are available
- Whether the mode uses an orchestrated multi-stage pipeline

Skills are loaded from `SKILL.md` files using a standard format (YAML front matter + markdown body). Built-in skills are embedded in the assembly; external skills from `ACP_SKILLS_DIR` can override or extend them.

### Component Map

| Component | Purpose |
|-----------|---------|
| `Program.cs` | Entry point — resolves API key, launches JSON-RPC server |
| `TornadoAcpRuntime` | Core runtime — manages sessions, skills, modes, tools, and config |
| `SkillRuntimeConfiguration` | Configures a single-agent session from an `AgentSkill` |
| `FileRefactoringOrchestrationConfiguration` | Orchestrated multi-stage refactoring pipeline |
| `SkillLoader` | Parses `SKILL.md` files into `AgentSkill` objects |
| `BuiltInSkills` | Provides the four default skill definitions |

### Session Lifecycle

1. **Initialize** — Server reports its capabilities (modes, config options, embedded context)
2. **New Session** — Client opens a session with a working directory; server loads the default skill ("agent") and creates a `ChatRuntime` with the appropriate `IRuntimeConfiguration`
3. **Prompt** — User messages are routed through the skill's agent (with tools if enabled); responses stream back as `session/update` notifications
4. **Mode Switch** — Client can switch modes (e.g. agent → plan); the server rebuilds the session runtime with the new skill's configuration
5. **Model Switch** — Client can change the OpenAI model via config options; the runtime is rebuilt with the new model

## Built-in Skills

### Agent (`agent`)

A coding assistant with full filesystem tool access. Reads files, searches the codebase, writes code, and makes surgical edits.

- **Tools**: `list_dir`, `search_files`, `read_file`, `write_file`, `replace_in_file`
- **Style**: Concise, direct, production-quality code
- **Best for**: Writing code, debugging, explaining behavior

### Chat (`chat`)

A general-purpose conversational assistant. No filesystem tools — purely knowledge-based.

- **Tools**: None
- **Style**: Clear explanations, markdown formatting, structured answers
- **Best for**: Questions, brainstorming, learning, comparing approaches

### Plan (`plan`)

An architecture advisor with full filesystem tool access. Explores existing code to inform design decisions and can prototype changes.

- **Tools**: `list_dir`, `search_files`, `read_file`, `write_file`, `replace_in_file`
- **Style**: Structured plans with rationale, numbered steps, risk analysis
- **Best for**: System design, implementation planning, architecture review

### Refactor (`refactor`)

An automated refactoring pipeline using an **orchestrated multi-stage process**:

```
User prompt ──► Analyze ──► Plan ──► Edit ──► Verify ──┐
                                       ▲               │
                                       └── retry ◄─────┘ (if FAIL, up to 2 attempts)
```

| Stage | Agent | Purpose |
|-------|-------|---------|
| **Analyze** | `RefactorAnalyze` | Identifies impacted files, symbols, dependencies, constraints |
| **Plan** | `RefactorPlan` | Creates an ordered list of edits with verification criteria |
| **Edit** | `RefactorEdit` | Applies changes using filesystem tools |
| **Verify** | `RefactorVerify` | Checks completeness; responds PASS or FAIL |
| **Finalize** | — | Returns the final result to the user |

If verification fails, the pipeline retries the Edit → Verify loop (up to 2 total attempts) with the verification feedback appended to the plan.

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OPENAI_API_KEY` | — | OpenAI API key (required unless `apiKey.json` exists) |
| `OPENAI_MODEL` | `gpt-4.1-nano` | Default model for completions |
| `ACP_SKILLS_DIR` | — | Directory to load additional/override skill files from |

### Available Models

| Model ID | Display Name | Notes |
|----------|-------------|-------|
| `gpt-5.2` | GPT-5.2 | Newest flagship model for complex coding and reasoning |
| `gpt-5.1` | GPT-5.1 | Strong coding and agentic model with configurable reasoning |
| `gpt-4.1-nano` | GPT-4.1 Nano | Fast and cheap, good for simple tasks |
| `gpt-4.1-mini` | GPT-4.1 Mini | Balanced speed and quality |
| `gpt-4.1` | GPT-4.1 | High quality, best for complex coding tasks |
| `o4-mini` | O4 Mini | Reasoning model, good for hard problems |
| `o3` | O3 | Advanced reasoning model |

The model can be changed at runtime through the IDE's config option UI.

## Custom Skills

### SKILL.md Format

Skills are defined in `.skill.md` files with YAML front matter and a markdown body:

```markdown
---
name: my-skill
display_name: My Custom Skill
description: Short description shown in the mode selector
use_tools: true
orchestrated: false
---
You are a specialized assistant for [domain].

## Core Responsibilities
- ...

## Tool Usage
- ...
```

### Front Matter Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Unique skill ID (lowercase alphanumeric, hyphens, underscores; max 64 chars) |
| `display_name` | string | No | Human-readable name (defaults to `name`) |
| `description` | string | No | Short description for the mode selector |
| `use_tools` | bool | No | Whether filesystem tools are provided (default: `false`) |
| `orchestrated` | bool | No | Whether to use the multi-stage pipeline (default: `false`) |

### Orchestrated Skills

For orchestrated skills (`orchestrated: true`), the markdown body can define per-stage instructions using `## stage:<name>` sections:

```markdown
---
name: my-pipeline
orchestrated: true
use_tools: true
---
General description of what this pipeline does.

## stage:analyze
Instructions for the analysis stage...

## stage:plan
Instructions for the planning stage...

## stage:edit
Instructions for the editing stage...

## stage:verify
Instructions for the verification stage...
```

Stage names must match the orchestration pipeline stages: `analyze`, `plan`, `edit`, `verify`.

### Loading External Skills

Place `.skill.md` files in a directory and set `ACP_SKILLS_DIR`:

```bash
export ACP_SKILLS_DIR=/path/to/my/skills
dotnet run --project src/LlmTornado.Acp.Server
```

External skills override built-in skills with the same `name`.

## Filesystem Tools

When a skill has `use_tools: true`, the agent gets access to these sandboxed filesystem operations:

| Tool | Parameters | Description |
|------|-----------|-------------|
| `list_dir` | `relativePath` | Lists files and folders in the working directory |
| `search_files` | `query`, `includePattern`, `maxResults` | Searches for text across files (supports globs like `*.cs`) |
| `read_file` | `relativePath`, `startLine`, `endLine` | Reads a range of lines from a file |
| `write_file` | `relativePath`, `content` | Writes full content to a file (creates directories as needed) |
| `replace_in_file` | `relativePath`, `oldText`, `newText` | Replaces exact text occurrences in a file |

All paths are sandboxed to the ACP root directory resolved from the session's working directory. Path traversal outside the sandbox is rejected.

## Project Structure

```
LlmTornado.Acp.Server/
├── Program.cs                                    # Entry point, API key resolution, server startup
├── TornadoAcpRuntime.cs                          # Core runtime: sessions, skills, tools, config
├── SkillRuntimeConfiguration.cs                  # IRuntimeConfiguration for single-agent skills
├── FileRefactoringModels.cs                      # Data models for refactoring pipeline stages
├── FileRefactoringOrchestrationConfiguration.cs  # Orchestration wiring for refactor pipeline
├── FileRefactoringRunnables.cs                   # Runnable implementations for each pipeline stage
└── Skills/
    ├── AgentSkill.cs                             # Skill data model
    ├── SkillLoader.cs                            # SKILL.md parser
    ├── BuiltInSkills.cs                          # Embedded default skills
    ├── agent.skill.md                            # Agent mode skill definition
    ├── chat.skill.md                             # Chat mode skill definition
    ├── plan.skill.md                             # Plan mode skill definition
    └── refactor.skill.md                         # Refactor mode skill definition
```

## Dependencies

- `LlmTornado` — Core LLM SDK
- `LlmTornado.Agents` — Agent orchestration framework (ChatRuntime, TornadoAgent)
- `LlmTornado.Acp` — ACP protocol models and JSON-RPC server

## License

MIT


