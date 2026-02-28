# Stage 5: MCP Configuration

## Goal

Load MCP server definitions from a JSON config file, construct `MCPServer` instances, initialize them (connect + fetch tools), and make their tools available to the agent. Failures are logged but non-fatal.

---

## Files to Create

### `src/LlmTornado.Cli/Mcp/McpConfigLoader.cs`
### `src/LlmTornado.Cli/Mcp/McpConfigModel.cs`

---

## Config File Resolution

Priority order:

1. `TORNADO_MCP_CONFIG` environment variable (absolute path)
2. `settings.json` → `mcp_config_path` field
3. `./mcp.json` relative to CWD (default)

```csharp
internal static string? ResolveMcpConfigPath(CliSettings settings)
{
    string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
    if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        return Path.GetFullPath(envPath);

    if (!string.IsNullOrEmpty(settings.McpConfigPath) && File.Exists(settings.McpConfigPath))
        return Path.GetFullPath(settings.McpConfigPath);

    string defaultPath = Path.GetFullPath("mcp.json");
    return File.Exists(defaultPath) ? defaultPath : null;
}
```

---

## Config File Format — `mcp.json`

Uses the same schema as `AcpMcpServerConfig` from the existing codebase, wrapped in a `servers` array:

```json
{
    "servers": [
        {
            "type": "stdio",
            "name": "filesystem",
            "command": "npx",
            "args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
            "env": {
                "NODE_ENV": "production"
            }
        },
        {
            "type": "stdio",
            "name": "playwright",
            "command": "npx",
            "args": ["@playwright/mcp@latest"]
        },
        {
            "type": "http",
            "name": "github",
            "url": "https://api.githubcopilot.com/mcp",
            "headers": {
                "Authorization": "Bearer ghp_xxxxxxxxxxxxx"
            }
        },
        {
            "type": "stdio",
            "name": "custom-tools",
            "command": "python",
            "args": ["my_tools_server.py"],
            "env": {
                "API_KEY": "${CUSTOM_API_KEY}"
            }
        }
    ]
}
```

---

## McpConfigModel — JSON Data Model

```csharp
namespace LlmTornado.Cli.Mcp;

using System.Text.Json.Serialization;

internal sealed class McpConfig
{
    [JsonPropertyName("servers")]
    public List<McpServerEntry> Servers { get; set; } = [];
}

internal sealed class McpServerEntry
{
    /// <summary>
    /// Transport type: "stdio" or "http"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "stdio";

    /// <summary>
    /// Unique server label (used for tool namespacing and display).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// For stdio: the command to run (e.g., "npx", "python", "docker").
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// For stdio: command arguments.
    /// </summary>
    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    /// <summary>
    /// For stdio: environment variables to set for the process.
    /// Values can reference other env vars with ${VAR_NAME} syntax.
    /// </summary>
    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>
    /// For http: the server URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// For http: additional connection headers (e.g., Authorization).
    /// Values can reference env vars with ${VAR_NAME} syntax.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional: only import these specific tools from the server.
    /// If null/empty, all tools are imported.
    /// </summary>
    [JsonPropertyName("allowed_tools")]
    public List<string>? AllowedTools { get; set; }
}
```

---

## McpConfigLoader — Implementation

```csharp
namespace LlmTornado.Cli.Mcp;

internal sealed class McpConfigLoader
{
    private readonly List<MCPServer> _servers = [];
    private readonly List<Tool> _allTools = [];
    private readonly List<McpServerStatus> _serverStatuses = [];

    /// <summary>
    /// All tools collected from all successfully initialized MCP servers.
    /// </summary>
    public IReadOnlyList<Tool> AllTools => _allTools;

    /// <summary>
    /// Status of each configured server (connected/failed/tool count).
    /// </summary>
    public IReadOnlyList<McpServerStatus> ServerStatuses => _serverStatuses;

    /// <summary>
    /// Load config file, construct MCPServer instances, initialize each.
    /// Failures are logged but non-fatal — other servers continue loading.
    /// </summary>
    public async Task LoadAsync(string configPath, Action<string>? log = null);

    /// <summary>
    /// Reinitialize all servers (disconnect + reconnect). Used by /mcp reload.
    /// </summary>
    public async Task ReloadAsync(Action<string>? log = null);

    /// <summary>
    /// Disconnect all MCP servers. Called on exit.
    /// </summary>
    public async Task DisposeAsync();
}

internal sealed class McpServerStatus
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool Connected { get; init; }
    public required int ToolCount { get; init; }
    public string? Error { get; init; }
    public List<string> ToolNames { get; init; } = [];
}
```

### Loading Flow

```csharp
public async Task LoadAsync(string configPath, Action<string>? log = null)
{
    // 1. Read and deserialize mcp.json
    string json = await File.ReadAllTextAsync(configPath);
    var config = JsonSerializer.Deserialize<McpConfig>(json);
    
    if (config?.Servers is null or { Count: 0 })
    {
        log?.Invoke("No MCP servers configured.");
        return;
    }

    // 2. For each server entry, construct MCPServer and initialize
    foreach (var entry in config.Servers)
    {
        try
        {
            // Resolve ${VAR_NAME} references in env and headers
            var resolvedEnv = ResolveEnvironmentVariables(entry.Env);
            var resolvedHeaders = ResolveEnvironmentVariables(entry.Headers);

            MCPServer server = entry.Type.ToLower() switch
            {
                "stdio" => new MCPServer(
                    serverLabel: entry.Name,
                    command: entry.Command ?? throw new InvalidOperationException($"stdio server '{entry.Name}' missing 'command'"),
                    arguments: entry.Args?.ToArray(),
                    environmentVariables: resolvedEnv,
                    allowedTools: entry.AllowedTools?.ToArray()
                ),
                "http" => new MCPServer(
                    serverLabel: entry.Name,
                    serverUrl: entry.Url ?? throw new InvalidOperationException($"http server '{entry.Name}' missing 'url'"),
                    allowedTools: entry.AllowedTools?.ToArray(),
                    additionalConnectionHeaders: resolvedHeaders
                ),
                _ => throw new InvalidOperationException($"Unknown server type '{entry.Type}' for '{entry.Name}'")
            };

            log?.Invoke($"  Connecting to MCP server '{entry.Name}' ({entry.Type})...");
            await server.InitializeAsync();

            _servers.Add(server);
            _allTools.AddRange(server.AllowedTornadoTools);
            _serverStatuses.Add(new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = true,
                ToolCount = server.AllowedTornadoTools.Count,
                ToolNames = server.AllowedTornadoTools.Select(t => t.Function?.Name ?? "?").ToList()
            });
            log?.Invoke($"  ✓ {entry.Name}: {server.AllowedTornadoTools.Count} tools loaded");
        }
        catch (Exception ex)
        {
            _serverStatuses.Add(new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = false,
                ToolCount = 0,
                Error = ex.Message
            });
            log?.Invoke($"  ✗ {entry.Name}: {ex.Message}");
        }
    }
}
```

### Environment Variable Resolution

Support `${VAR_NAME}` syntax in `env` values and `headers` values:

```csharp
private static Dictionary<string, string>? ResolveEnvironmentVariables(
    Dictionary<string, string>? input)
{
    if (input is null) return null;
    
    var resolved = new Dictionary<string, string>();
    foreach (var (key, value) in input)
    {
        resolved[key] = Regex.Replace(value, @"\$\{(\w+)\}", match =>
        {
            string envVarName = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(envVarName) ?? match.Value;
        });
    }
    return resolved;
}
```

This allows config files to reference secrets without hardcoding them:

```json
{
    "type": "http",
    "name": "github",
    "url": "https://api.githubcopilot.com/mcp",
    "headers": {
        "Authorization": "Bearer ${GITHUB_TOKEN}"
    }
}
```

---

## Tool Naming for MCP Tools

MCP tools from `MCPServer.AllowedTornadoTools` retain their original names from the MCP server. To distinguish them in the tool approval system, the `ToolApprovalManager` (Stage 6) uses the `mcp:{server_name}:{tool_name}` key format. The `Tool` objects themselves keep their native MCP names for proper invocation.

---

## Integration Points

| Consumer | What it uses |
|----------|-------------|
| `CliAgentBuilder` (Stage 8) | `AllTools` — adds all MCP tools to the agent via `agent.AddTool()` |
| `ToolApprovalManager` (Stage 6) | Tool names with `mcp:` prefix for approval tracking |
| `/mcp list` command (Stage 9) | `ServerStatuses` for display |
| `/mcp reload` command (Stage 9) | `ReloadAsync()` to reconnect |
| `Program.cs` (Stage 10) | `DisposeAsync()` on shutdown |

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| `mcp.json` not found | Skip MCP loading entirely, log info message |
| `mcp.json` has invalid JSON | Log error with file path, skip MCP |
| Individual server fails to connect | Log error for that server, continue with others |
| Server disconnects mid-session | Tool calls to that server will fail; error returned to agent as tool result |
| `/mcp reload` with same config | Disconnects existing, re-reads config, reconnects |

---

## Types Used from LlmTornado

| Type | Namespace | Purpose |
|------|-----------|---------|
| `MCPServer` | `LlmTornado.Mcp` | MCP server connection + tool discovery |
| `Tool` | `LlmTornado.Chat` | Tool objects converted from MCP |
| `McpClientTool` | `LlmTornado.Mcp` | Raw MCP tool (before conversion) |
