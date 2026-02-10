# LlmTornado.Acp

Integrate LlmTornado agents with any IDE or coding assistant via the **Agent Client Protocol (ACP)** — an open, JSON-RPC 2.0 based standard for communication between AI agents and client applications.

## Overview

ACP acts as a universal bridge between AI coding agents and editors (Zed, JetBrains, Neovim, etc.), similar to what LSP does for language tooling. This package provides:

- **ACP protocol models** — Typed C# representations of all ACP messages, capabilities, and content blocks
- **ChatRuntime integration** — Bridge between ACP sessions and LlmTornado's agent orchestration system
- **JSON-RPC 2.0 server** — Ready-to-use stdio transport for serving ACP-compliant agents

## Quick Start

### 1. Create an ACP Agent

```csharp
using LlmTornado.Acp;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;

public class MyAcpAgent : BaseAcpTornadoRuntimeConfiguration
{
    public MyAcpAgent(IRuntimeConfiguration runtimeConfig)
        : base(runtimeConfig, "my-agent", "1.0.0")
    {
    }

    protected override AcpAgentCapabilities DescribeCapabilities()
    {
        return new AcpAgentCapabilities
        {
            PromptCapabilities = new AcpPromptCapabilities
            {
                Image = true,
                Audio = false,
                EmbeddedContext = true
            }
        };
    }
}
```

### 2. Run the JSON-RPC Server

```csharp
// Create and configure your agent runtime
var runtimeConfig = new SingletonRuntimeConfiguration(agent);
var acpAgent = new MyAcpAgent(runtimeConfig);

// Start the ACP server over stdio
var server = new AcpJsonRpcServer(acpAgent);
await server.RunAsync();
```

### 3. Configure Your Editor

#### Zed
```json
{
  "agent": {
    "command": "dotnet",
    "args": ["run", "--project", "path/to/your/agent"]
  }
}
```

#### JetBrains
```json
{
  "mcpServers": {},
  "acpAgents": {
    "my-agent": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/your/agent"]
    }
  }
}
```

## Protocol Flow

```
Client (IDE)                          Agent (LlmTornado)
    │                                        │
    │──── initialize ───────────────────────►│
    │◄─── capabilities, auth methods ────────│
    │                                        │
    │──── session/new ──────────────────────►│
    │◄─── sessionId ─────────────────────────│
    │                                        │
    │──── session/prompt ───────────────────►│
    │◄─── session/update (streaming) ────────│
    │◄─── session/update (streaming) ────────│
    │◄─── promptResponse (stopReason) ───────│
    │                                        │
    │──── session/cancel ───────────────────►│
    │                                        │
```

## Architecture

| Component | Purpose |
|-----------|---------|
| `AcpJsonRpcServer` | JSON-RPC 2.0 transport layer over stdio |
| `IAcpRuntimeConfiguration` | Interface for ACP agent implementations |
| `BaseAcpTornadoRuntimeConfiguration` | Base class integrating with ChatRuntime |
| `AcpTornadoExtension` | Bidirectional type conversion (ACP ↔ LlmTornado) |
| `AcpModels.*` | Typed representations of ACP protocol messages |

## Key Features

- **Session management** — Multiple concurrent sessions with independent state
- **Streaming updates** — Real-time agent output via `session/update` notifications
- **Capability negotiation** — Client and agent exchange supported features on initialization
- **Tool call reporting** — Report tool executions with status, content, and file locations
- **Execution plans** — Share multi-step plans with priority and status tracking
- **Cancellation support** — Cancel ongoing operations via `session/cancel`

## Dependencies

- `LlmTornado` — Core LLM SDK
- `LlmTornado.Agents` — Agent orchestration framework

## License

MIT
