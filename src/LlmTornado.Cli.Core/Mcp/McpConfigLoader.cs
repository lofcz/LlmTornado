using System.Text.Json;
using System.Text.RegularExpressions;
using LlmTornado.Common;
using LlmTornado.Mcp;

namespace LlmTornado.Cli.Core.Mcp;

/// <summary>
/// Loads MCP server definitions from JSON config files and initializes them.
/// Supports both a global config (%APPDATA%/llmtornado/mcp.json) and a project-local config.
/// </summary>
public sealed partial class McpConfigLoader : IAsyncDisposable
{
    private readonly List<MCPServer> _servers = [];
    private readonly List<Tool> _allTools = [];
    private readonly List<McpServerStatus> _serverStatuses = [];
    private string? _localConfigPath;
    private string? _globalConfigPath;

    [GeneratedRegex(@"\$\{(\w+)\}")]
    private static partial Regex EnvVarPattern();

    public IReadOnlyList<Tool> AllTools => _allTools;
    public IReadOnlyList<McpServerStatus> ServerStatuses => _serverStatuses;

    /// <summary>
    /// Resolve the project-local MCP config file path.
    /// Returns null if none found.
    /// </summary>
    public static string? ResolveMcpConfigPath(string? mcpConfigPathOverride)
    {
        if (!string.IsNullOrEmpty(mcpConfigPathOverride) && File.Exists(mcpConfigPathOverride))
            return Path.GetFullPath(mcpConfigPathOverride);

        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return Path.GetFullPath(envPath);

        string defaultPath = Path.GetFullPath("mcp.json");
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>
    /// Get the path where the local mcp.json should live, whether it exists or not.
    /// </summary>
    public static string ResolveDefaultMcpConfigPath(string? mcpConfigPathOverride)
    {
        if (!string.IsNullOrEmpty(mcpConfigPathOverride))
            return Path.GetFullPath(mcpConfigPathOverride);

        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
            return Path.GetFullPath(envPath);

        return Path.GetFullPath("mcp.json");
    }

    /// <summary>
    /// Resolve the global MCP config path.
    /// Checks <c>TORNADO_MCP_GLOBAL_CONFIG</c> env var first; if set and the file exists, uses it.
    /// Otherwise falls back to <c>%APPDATA%/llmtornado/mcp.json</c>.
    /// Returns null if neither exists.
    /// </summary>
    public static string? ResolveGlobalMcpConfigPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_GLOBAL_CONFIG");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return Path.GetFullPath(envPath);

        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "llmtornado", "mcp.json");
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>
    /// Get the path where the global mcp.json should live, whether it exists or not.
    /// </summary>
    public static string ResolveDefaultGlobalMcpConfigPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("TORNADO_MCP_GLOBAL_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
            return Path.GetFullPath(envPath);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "llmtornado", "mcp.json");
    }

    /// <summary>
    /// Load config and initialize all MCP servers from a single (local) config file.
    /// </summary>
    public async Task LoadAsync(string configPath, Action<string>? log = null)
    {
        _localConfigPath = configPath;
        await LoadMergedAsync(log);
    }

    /// <summary>
    /// Load config and initialize all MCP servers from both global and local config files.
    /// Local entries shadow global entries with the same name.
    /// </summary>
    public async Task LoadAsync(string? localConfigPath, string? globalConfigPath, Action<string>? log = null)
    {
        _localConfigPath = localConfigPath;
        _globalConfigPath = globalConfigPath;
        await LoadMergedAsync(log);
    }

    /// <summary>
    /// Reload all servers from both config files.
    /// </summary>
    public async Task ReloadAsync(Action<string>? log = null)
    {
        await DisposeAsync();
        _servers.Clear();
        _allTools.Clear();
        _serverStatuses.Clear();
        await LoadMergedAsync(log);
    }

    /// <summary>
    /// Switch to a new local config path, keeping the global path unchanged.
    /// Disposes existing servers first. If the new path does not exist, only global servers load.
    /// </summary>
    public async Task LoadFromPathAsync(string? newLocalConfigPath, Action<string>? log = null)
    {
        await DisposeAsync();
        _servers.Clear();
        _allTools.Clear();
        _serverStatuses.Clear();

        _localConfigPath = newLocalConfigPath;
        await LoadMergedAsync(log);
    }

    /// <summary>
    /// Core merge logic: load global config first, then local; local shadows by name.
    /// </summary>
    private async Task LoadMergedAsync(Action<string>? log = null)
    {
        Dictionary<string, (McpServerEntry Entry, McpServerSource Source)> merged = new(StringComparer.OrdinalIgnoreCase);

        // 1. Load global config first (lower precedence)
        if (_globalConfigPath is not null && File.Exists(_globalConfigPath))
        {
            McpConfig? globalConfig = await ReadConfigFromFileAsync(_globalConfigPath, log);
            if (globalConfig is not null)
            {
                foreach (McpServerEntry entry in globalConfig.Servers)
                {
                    entry.Source = McpServerSource.Global;
                    merged[entry.Name] = (entry, McpServerSource.Global);
                }
            }
        }

        // 2. Load local config — shadows global entries with the same name
        if (_localConfigPath is not null && File.Exists(_localConfigPath))
        {
            McpConfig? localConfig = await ReadConfigFromFileAsync(_localConfigPath, log);
            if (localConfig is not null)
            {
                foreach (McpServerEntry entry in localConfig.Servers)
                {
                    entry.Source = McpServerSource.Local;
                    merged[entry.Name] = (entry, McpServerSource.Local);
                }
            }
        }

        if (merged.Count == 0)
        {
            log?.Invoke("  No MCP servers configured.");
            return;
        }

        // 3. Initialize all merged servers
        foreach ((McpServerEntry entry, McpServerSource source) in merged.Values)
        {
            await InitializeServer(entry, source, log);
        }
    }

    private static async Task<McpConfig?> ReadConfigFromFileAsync(string path, Action<string>? log)
    {
        try
        {
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<McpConfig>(json);
        }
        catch (JsonException ex)
        {
            log?.Invoke($"  Failed to parse MCP config at {path}: {ex.Message}");
            return null;
        }
    }

    private async Task InitializeServer(McpServerEntry entry, McpServerSource source, Action<string>? log)
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
                string workingDirectory = ResolveEnvVarString(entry.Cwd ?? "");
                server = new MCPServer(entry.Name, command, args, workingDirectory: workingDirectory, environmentVariables: env, allowedTools: allowed);
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
                Source = source,
            });

            log?.Invoke($"  ✓ {entry.Name} ({entry.Type}, {source}) — {server.AllowedTornadoTools.Count} tools");
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
                Source = source,
            });

            log?.Invoke($"  ✗ {entry.Name} ({entry.Type}, {source}) — {ex.Message}");
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

    /// <summary>
    /// Get the local config path (set after <see cref="LoadAsync(string, Action{string}?)"/> is called).
    /// </summary>
    public string? ConfigPath => _localConfigPath;

    /// <summary>
    /// Get the global config path.
    /// </summary>
    public string? GlobalConfigPath => _globalConfigPath;

    /// <summary>
    /// Read and deserialize the local mcp.json config from disk.
    /// Returns null if the file doesn't exist or can't be parsed.
    /// </summary>
    public McpConfig? ReadConfig()
    {
        return ReadConfigFromFile(_localConfigPath);
    }

    /// <summary>
    /// Read and deserialize the global mcp.json config from disk.
    /// </summary>
    public McpConfig? ReadGlobalConfig()
    {
        return ReadConfigFromFile(_globalConfigPath);
    }

    /// <summary>
    /// Read and deserialize either the local or global config.
    /// </summary>
    public McpConfig? ReadConfig(McpServerSource scope)
    {
        return scope switch
        {
            McpServerSource.Global => ReadConfigFromFile(_globalConfigPath),
            McpServerSource.Local => ReadConfigFromFile(_localConfigPath),
            _ => null
        };
    }

    private static McpConfig? ReadConfigFromFile(string? path)
    {
        if (path is null || !File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<McpConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serialize and write a config to the specified scope's config path.
    /// </summary>
    public async Task SaveConfigAsync(McpConfig config, McpServerSource scope)
    {
        string path = scope switch
        {
            McpServerSource.Global => _globalConfigPath ?? ResolveDefaultGlobalMcpConfigPath(),
            _ => _localConfigPath ?? ResolveDefaultMcpConfigPath(null)
        };

        string? dir = Path.GetDirectoryName(path);
        if (dir is not null) Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        // Keep path references in sync
        if (scope == McpServerSource.Global)
            _globalConfigPath = path;
        else
            _localConfigPath = path;
    }

    /// <summary>
    /// Serialize and write a config to the local config path (backwards compat).
    /// </summary>
    public async Task SaveConfigAsync(McpConfig config)
    {
        await SaveConfigAsync(config, McpServerSource.Local);
    }

    /// <summary>
    /// Create a default empty mcp.json at the given path if it doesn't exist.
    /// </summary>
    public static async Task CreateDefaultConfigIfMissingAsync(string path)
    {
        if (File.Exists(path)) return;

        string? dir = Path.GetDirectoryName(path);
        if (dir is not null) Directory.CreateDirectory(dir);

        McpConfig empty = new() { Servers = [] };
        string json = JsonSerializer.Serialize(empty, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// Test connectivity to a single MCP server entry without persisting it.
    /// Returns a <see cref="McpServerStatus"/> with the result.
    /// </summary>
    public static async Task<McpServerStatus> TestConnectionAsync(McpServerEntry entry)
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
                string workingDirectory = ResolveEnvVarString(entry.Cwd ?? "");
                server = new MCPServer(entry.Name, command, args, workingDirectory: workingDirectory, environmentVariables: env, allowedTools: allowed);
            }

            await server.InitializeAsync();
            int toolCount = server.AllowedTornadoTools.Count;

            try
            {
                if (server.McpClient is not null)
                    await server.McpClient.DisposeAsync();
            }
            catch { }

            return new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = true,
                ToolCount = toolCount,
            };
        }
        catch (Exception ex)
        {
            return new McpServerStatus
            {
                Name = entry.Name,
                Type = entry.Type,
                Connected = false,
                ToolCount = 0,
                Error = ex.Message,
            };
        }
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
