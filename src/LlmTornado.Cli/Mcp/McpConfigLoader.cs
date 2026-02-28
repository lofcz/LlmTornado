using System.Text.Json;
using System.Text.RegularExpressions;
using LlmTornado.Common;
using LlmTornado.Mcp;

namespace LlmTornado.Cli.Mcp;

/// <summary>
/// Loads MCP server definitions from a JSON config file and initializes them.
/// </summary>
internal sealed partial class McpConfigLoader : IAsyncDisposable
{
    private readonly List<MCPServer> _servers = [];
    private readonly List<Tool> _allTools = [];
    private readonly List<McpServerStatus> _serverStatuses = [];
    private string? _configPath;

    [GeneratedRegex(@"\$\{(\w+)\}")]
    private static partial Regex EnvVarPattern();

    public IReadOnlyList<Tool> AllTools => _allTools;
    public IReadOnlyList<McpServerStatus> ServerStatuses => _serverStatuses;

    /// <summary>
    /// Resolve the MCP config file path. Returns null if none found.
    /// </summary>
    public static string? ResolveMcpConfigPath(CliSettings settings)
    {
        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return Path.GetFullPath(envPath);

        if (!string.IsNullOrEmpty(settings.McpConfigPath) && File.Exists(settings.McpConfigPath))
            return Path.GetFullPath(settings.McpConfigPath);

        string defaultPath = Path.GetFullPath("mcp.json");
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>
    /// Get the path where the mcp.json should live, whether it exists or not.
    /// Prefers settings override, then env var, then ./mcp.json.
    /// </summary>
    public static string ResolveDefaultMcpConfigPath(CliSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.McpConfigPath))
            return Path.GetFullPath(settings.McpConfigPath);

        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
            return Path.GetFullPath(envPath);

        return Path.GetFullPath("mcp.json");
    }

    /// <summary>
    /// Load config and initialize all MCP servers.
    /// </summary>
    public async Task LoadAsync(string configPath, Action<string>? log = null)
    {
        _configPath = configPath;

        string json = await File.ReadAllTextAsync(configPath);
        McpConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<McpConfig>(json);
        }
        catch (JsonException ex)
        {
            log?.Invoke($"  Failed to parse MCP config: {ex.Message}");
            return;
        }

        if (config is null || config.Servers.Count == 0)
        {
            log?.Invoke("  No MCP servers configured.");
            return;
        }

        foreach (McpServerEntry entry in config.Servers)
        {
            await InitializeServer(entry, log);
        }
    }

    /// <summary>
    /// Reload all servers.
    /// </summary>
    public async Task ReloadAsync(Action<string>? log = null)
    {
        await DisposeAsync();
        _servers.Clear();
        _allTools.Clear();
        _serverStatuses.Clear();

        if (_configPath is not null)
            await LoadAsync(_configPath, log);
    }

    private async Task InitializeServer(McpServerEntry entry, Action<string>? log)
    {
        try
        {
            MCPServer server;
            string[] allowedTools = entry.AllowedTools?.ToArray() ?? [];
            string[]? allowed = allowedTools.Length > 0 ? allowedTools : null;

            if (entry.Type.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, string>? headers = ResolveEnvVars(entry.Headers);
                string url = ResolveEnvVarString(entry.Url ?? "");
                server = new MCPServer(entry.Name, url, allowed, headers);
            }
            else
            {
                string command = ResolveEnvVarString(entry.Command ?? "");
                string[]? args = entry.Args?.Select(ResolveEnvVarString).ToArray();
                Dictionary<string, string>? env = ResolveEnvVars(entry.Env);
                server = new MCPServer(entry.Name, command, args, environmentVariables: env, allowedTools: allowed);
            }

            await server.InitializeAsync();
            _servers.Add(server);

            foreach (Tool tool in server.AllowedTornadoTools)
            {
                _allTools.Add(tool);
            }

            _serverStatuses.Add(new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = true,
                ToolCount = server.AllowedTornadoTools.Count,
            });

            log?.Invoke($"  ✓ {entry.Name} ({entry.Type}) — {server.AllowedTornadoTools.Count} tools");
        }
        catch (Exception ex)
        {
            _serverStatuses.Add(new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = false,
                ToolCount = 0,
                Error = ex.Message,
            });

            log?.Invoke($"  ✗ {entry.Name} ({entry.Type}) — {ex.Message}");
        }
    }

    private static string ResolveEnvVarString(string value)
    {
        return EnvVarPattern().Replace(value, match =>
        {
            string varName = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(varName) ?? match.Value;
        });
    }

    private static Dictionary<string, string>? ResolveEnvVars(Dictionary<string, string>? dict)
    {
        if (dict is null || dict.Count == 0)
            return dict;

        Dictionary<string, string> resolved = new();
        foreach ((string key, string value) in dict)
        {
            resolved[key] = ResolveEnvVarString(value);
        }
        return resolved;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (MCPServer server in _servers)
        {
            try
            {
                if (server.McpClient is not null)
                    await server.McpClient.DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
