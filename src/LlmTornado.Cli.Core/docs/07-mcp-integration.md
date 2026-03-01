# 07 — MCP Server Integration

The MCP (Model Context Protocol) integration loads tool-providing servers from a JSON config file, initializes them (stdio or HTTP), and exposes their tools to the agent.

## Architecture

```mermaid
classDiagram
    class McpConfigLoader {
        -List~MCPServer~ _servers
        -List~Tool~ _allTools
        -List~McpServerStatus~ _serverStatuses
        -string _configPath
        +AllTools: IReadOnlyList~Tool~
        +ServerStatuses: IReadOnlyList~McpServerStatus~
        +ResolveMcpConfigPath(override) string$
        +ResolveDefaultMcpConfigPath(override) string$
        +LoadAsync(configPath, log) Task
        +ReloadAsync(log) Task
        +DisposeAsync() ValueTask
    }

    class McpConfig {
        +List~McpServerEntry~ Servers
    }

    class McpServerEntry {
        +string Type
        +string Name
        +string Command
        +List~string~ Args
        +Dictionary Env
        +string Url
        +Dictionary Headers
        +List~string~ AllowedTools
    }

    class McpServerStatus {
        +string Name
        +string Type
        +bool Connected
        +int ToolCount
        +string Error
    }

    McpConfigLoader --> McpConfig
    McpConfig --> McpServerEntry
    McpConfigLoader --> McpServerStatus
```

## Config File Resolution

```mermaid
flowchart TD
    Start["Resolve MCP config path"] --> Override{"Override path<br/>provided?"}
    Override -->|"Yes, exists"| Use1["Use override path"]
    Override -->|"No"| Env{"TORNADO_MCP_CONFIG<br/>env var set?"}
    Env -->|"Yes, exists"| Use2["Use env var path"]
    Env -->|"No"| Default{"./mcp.json<br/>exists?"}
    Default -->|"Yes"| Use3["Use ./mcp.json"]
    Default -->|"No"| Null["No config found<br/>(no MCP servers)"]
```

## Config File Format

The MCP configuration is a JSON file listing servers to connect to:

```json
{
  "servers": [
    {
      "type": "stdio",
      "name": "filesystem",
      "command": "npx",
      "args": ["-y", "@anthropic/mcp-server-filesystem", "/path/to/allowed"],
      "env": {
        "NODE_ENV": "production"
      },
      "allowedTools": ["read_file", "write_file", "list_directory"]
    },
    {
      "type": "http",
      "name": "custom-api",
      "url": "https://mcp.example.com/v1",
      "headers": {
        "Authorization": "Bearer ${MCP_API_TOKEN}"
      }
    }
  ]
}
```

### Server Entry Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | Yes | `"stdio"` or `"http"` |
| `name` | string | Yes | Display name for logging |
| `command` | string | stdio only | Command to execute |
| `args` | string[] | No | Command arguments |
| `env` | object | No | Environment variables for the process |
| `url` | string | http only | Server URL |
| `headers` | object | No | HTTP headers |
| `allowedTools` | string[] | No | Whitelist of tool names (null = all) |

### Environment Variable Expansion

All string values in the config support `${VAR_NAME}` expansion:

```mermaid
flowchart LR
    Input["'Bearer ${MCP_API_TOKEN}'"] --> Regex["Regex: \\$\\{(\\w+)\\}"]
    Regex --> Lookup["Environment.GetEnvironmentVariable('MCP_API_TOKEN')"]
    Lookup --> Replace["'Bearer sk-abc123...'"]
```

This applies to `command`, `args`, `url`, `env` values, and `headers` values.

## Server Initialization

```mermaid
sequenceDiagram
    participant MCL as McpConfigLoader
    participant Config as mcp.json
    participant Server as MCPServer
    participant Tools as Tool List

    MCL->>Config: Read & parse JSON
    
    loop Each server entry
        alt type = "stdio"
            MCL->>Server: new MCPServer(name, command, args, env, allowedTools)
        else type = "http"
            MCL->>Server: new MCPServer(name, url, allowedTools, headers)
        end
        
        MCL->>Server: InitializeAsync()
        
        alt Success
            Server-->>MCL: Connected
            MCL->>Tools: Add server.AllowedTornadoTools
            MCL->>MCL: Status: ✓ connected, N tools
        else Failure
            Server-->>MCL: Exception
            MCL->>MCL: Status: ✗ error message
        end
    end
```

## Server Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Created: new McpConfigLoader()
    Created --> Loading: LoadAsync(configPath)
    Loading --> Active: Servers initialized
    Loading --> PartialFail: Some servers failed
    PartialFail --> Active: Working servers available

    Active --> Reloading: ReloadAsync()
    Reloading --> Disposing: DisposeAsync() all servers
    Disposing --> Loading: Re-load from config

    Active --> Disposed: DisposeAsync()
    Disposed --> [*]
```

### Reload Process

`ReloadAsync()` performs a full tear-down and re-initialization:

1. `DisposeAsync()` — disconnect all servers
2. Clear internal lists (servers, tools, statuses)
3. `LoadAsync()` — re-read config and reconnect

This supports live-reloading when the config file changes.

## Tool Collection & Integration

MCP tools are collected after initialization and exposed via `AllTools`:

```mermaid
flowchart TD
    S1["Server 1: filesystem<br/>3 tools"] --> Collect["McpConfigLoader.AllTools"]
    S2["Server 2: custom-api<br/>5 tools"] --> Collect
    S3["Server 3: search<br/>2 tools"] --> Collect

    Collect --> AB["AgentBuilder.CollectTools()"]
    AB --> Merge["Merged with built-in +<br/>skill script + additional tools"]
    Merge --> Filter["Persona tool filter"]
    Filter --> Agent["TornadoAgent tools"]
```

### Tool Name Resolution

MCP tools use the `MCPServer`'s tool name format. The `AgentBuilder` uses `tool.ResolvedName` (which maps to `Function.Name` for MCP tools) for:
- Tool permission registration
- Tool optimizer identification
- Persona whitelist/blacklist matching

## Error Handling

Each server is initialized independently. A failure in one server does not affect others:

```mermaid
flowchart TD
    Init["Initialize all servers"] --> S1["Server 1: ✓ Connected<br/>3 tools loaded"]
    Init --> S2["Server 2: ✗ Connection refused"]
    Init --> S3["Server 3: ✓ Connected<br/>5 tools loaded"]

    S1 --> Available["Available: 8 tools<br/>from 2 servers"]
    S2 --> Status["Status tracked:<br/>filesystem: connected<br/>custom-api: error<br/>search: connected"]
    S3 --> Available
```

The `McpServerStatus` list tracks the state of each server for diagnostic display.

## Resource Cleanup

`McpConfigLoader` implements `IAsyncDisposable`. On disposal:

1. Iterates all connected `MCPServer` instances
2. Calls `server.McpClient.DisposeAsync()` on each
3. Best-effort — exceptions are swallowed to ensure all servers are attempted

```csharp
// Usage pattern
await using McpConfigLoader mcpLoader = new();
await mcpLoader.LoadAsync(configPath);
// ... use tools ...
// Disposed automatically at end of scope
```
