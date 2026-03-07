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
    private readonly Dictionary<string, McpServerSource> _toolSourceMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _toolServerMap = new(StringComparer.OrdinalIgnoreCase);
    private AgentSettings _settings = new();
    private McpSessionPolicy? _sessionPolicy;
    private string? _localConfigPath;
    private string? _globalConfigPath;

    [GeneratedRegex(@"\$\{(\w+)\}")]
    private static partial Regex EnvVarPattern();

    public IReadOnlyList<Tool> AllTools => _allTools;
    public IReadOnlyList<McpServerStatus> ServerStatuses => _serverStatuses;

    /// <summary>
    /// Maps each tool name to the MCP server source (Global/Local) it came from.
    /// </summary>
    public IReadOnlyDictionary<string, McpServerSource> ToolSourceMap => _toolSourceMap;

    /// <summary>
    /// Maps each tool name to the MCP server label it came from.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToolServerMap => _toolServerMap;

    /// <summary>
    /// Resolve the project-local MCP config file path.
    /// Returns null if none found.
    /// </summary>
    public void Configure(AgentSettings settings, McpSessionPolicy? sessionPolicy)
    {
        _settings = settings;
        _sessionPolicy = sessionPolicy;
    }

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
        _toolSourceMap.Clear();
        _toolServerMap.Clear();
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
        _toolSourceMap.Clear();
        _toolServerMap.Clear();

        _localConfigPath = newLocalConfigPath;
        await LoadMergedAsync(log);
    }

    /// <summary>
    /// Core merge logic: load global config first, then local; local shadows by name.
    /// </summary>
    private async Task LoadMergedAsync(Action<string>? log = null)
    {
        Dictionary<string, (McpServerEntry Entry, McpServerSource Source)> merged = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> reservedNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (BuiltInMcpServerDefinition builtIn in BuiltInMcpServerCatalog.GetDefinitions(_sessionPolicy?.WorkingDirectory))
        {
            reservedNames.Add(builtIn.Name);
            merged[builtIn.Name] = (CloneEntry(builtIn.Entry), McpServerSource.BuiltIn);
        }

        // 1. Load global config first (lower precedence)
        if (_globalConfigPath is not null && File.Exists(_globalConfigPath))
        {
            McpConfig? globalConfig = await ReadConfigFromFileAsync(_globalConfigPath, log);
            if (globalConfig is not null)
            {
                foreach (McpServerEntry entry in globalConfig.Servers)
                {
                    if (reservedNames.Contains(entry.Name))
                    {
                        log?.Invoke($"  Skipping global MCP server '{entry.Name}' because the name is reserved by a built-in server.");
                        continue;
                    }

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
                    if (reservedNames.Contains(entry.Name))
                    {
                        log?.Invoke($"  Skipping local MCP server '{entry.Name}' because the name is reserved by a built-in server.");
                        continue;
                    }

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
            if (_settings.DisabledMcpServers.Contains(entry.Name))
            {
                _serverStatuses.Add(BuildStatus(entry, source, connected: false, toolCount: 0,
                    enabled: false, error: "Disabled in session settings."));
                continue;
            }

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
            List<Tool> filteredTools;
            string[] allowedTools = ApplyDisabledToolFilter(entry).ToArray();
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
                string workingDirectory = ResolveWorkingDirectory(entry.Cwd);
                server = new MCPServer(entry.Name, command, args, workingDirectory: workingDirectory, environmentVariables: env, allowedTools: allowed);
            }

            await server.InitializeAsync();

            if (source == McpServerSource.BuiltIn)
                await ConfigureBuiltInServerAsync(server, entry.Name, log);

            _servers.Add(server);
            filteredTools = FilterAndWrapTools(server.AllowedTornadoTools, entry.Name);

            foreach (Tool tool in filteredTools)
            {
                _allTools.Add(tool);
                string toolName = tool.ResolvedName;
                if (!string.IsNullOrEmpty(toolName))
                {
                    _toolSourceMap.TryAdd(toolName, source);
                    _toolServerMap.TryAdd(toolName, entry.Name);
                }
            }

            _serverStatuses.Add(BuildStatus(entry, source, connected: true, toolCount: filteredTools.Count, enabled: true));

            log?.Invoke($"  ✓ {entry.Name} ({entry.Type}, {source}) — {filteredTools.Count} tools");
        }
        catch (Exception ex)
        {
            _serverStatuses.Add(BuildStatus(entry, source, connected: false, toolCount: 0, enabled: true, error: ex.Message));

            log?.Invoke($"  ✗ {entry.Name} ({entry.Type}, {source}) — {ex.Message}");
        }
    }

    private static McpServerEntry CloneEntry(McpServerEntry entry)
    {
        return new McpServerEntry
        {
            Type = entry.Type,
            Name = entry.Name,
            Command = entry.Command,
            Args = entry.Args is null ? null : [.. entry.Args],
            Env = entry.Env is null ? null : new Dictionary<string, string>(entry.Env, StringComparer.OrdinalIgnoreCase),
            Cwd = entry.Cwd,
            Url = entry.Url,
            Headers = entry.Headers is null ? null : new Dictionary<string, string>(entry.Headers, StringComparer.OrdinalIgnoreCase),
            AllowedTools = entry.AllowedTools is null ? null : [.. entry.AllowedTools],
            Source = entry.Source
        };
    }

    private McpServerStatus BuildStatus(McpServerEntry entry, McpServerSource source, bool connected, int toolCount, bool enabled, string? error = null)
    {
        BuiltInMcpServerDefinition? builtIn = source == McpServerSource.BuiltIn
            ? BuiltInMcpServerCatalog.GetDefinition(entry.Name, _sessionPolicy?.WorkingDirectory)
            : null;

        return new McpServerStatus
        {
            Name = entry.Name,
            Type = entry.Type,
            Description = builtIn?.Description,
            Connected = connected,
            ToolCount = toolCount,
            Enabled = enabled,
            Error = error,
            Source = source,
        };
    }

    private List<string> ApplyDisabledToolFilter(McpServerEntry entry)
    {
        List<string> allowedTools = entry.AllowedTools is null ? [] : [.. entry.AllowedTools];
        if (!_settings.DisabledMcpTools.TryGetValue(entry.Name, out HashSet<string>? disabledTools) || disabledTools.Count == 0)
            return allowedTools;

        if (allowedTools.Count == 0)
            return allowedTools;

        return [.. allowedTools.Where(tool => !disabledTools.Contains(tool))];
    }

    private string ResolveWorkingDirectory(string? configuredWorkingDirectory)
    {
        string candidate = ResolveEnvVarString(configuredWorkingDirectory ?? string.Empty);
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = _sessionPolicy?.WorkingDirectory ?? Directory.GetCurrentDirectory();

        if (!Path.IsPathRooted(candidate))
            candidate = Path.Combine(_sessionPolicy?.WorkingDirectory ?? Directory.GetCurrentDirectory(), candidate);

        return Path.GetFullPath(candidate);
    }

    private List<Tool> FilterAndWrapTools(List<Tool> tools, string serverName)
    {
        HashSet<string> disabled = _settings.DisabledMcpTools.TryGetValue(serverName, out HashSet<string>? names)
            ? names
            : [];

        List<Tool> filtered = [];
        foreach (Tool tool in tools)
        {
            if (disabled.Contains(tool.ResolvedName))
                continue;

            filtered.Add(WrapToolForPolicy(tool, serverName));
        }

        return filtered;
    }

    private Tool WrapToolForPolicy(Tool tool, string serverName)
    {
        if (_sessionPolicy is null || tool.RemoteTool is null)
            return tool;

        if (!serverName.Equals(BuiltInMcpServerCatalog.DesktopCommanderServerName, StringComparison.OrdinalIgnoreCase))
            return tool;

        string toolName = tool.ResolvedName;
        if (!toolName.Equals("start_process", StringComparison.OrdinalIgnoreCase))
            return tool;

        ToolFunction function = tool.Function is null
            ? new ToolFunction(toolName, tool.ResolvedDescription)
            : new ToolFunction(tool.Function.Name, tool.Function.Description, tool.Function.Parameters ?? new { });

        McpTool remote = new()
        {
            CallAsync = async (args, progress, serializer, fillContent, ct) =>
            {
                string? command = TryGetStringArg(args, "command", "cmd");
                if (!_sessionPolicy.IsCommandAllowed(command))
                {
                    return new FunctionResult(toolName,
                        $"Command '{command}' is blocked by the current session policy.",
                        FunctionResultSetContentModes.Passthrough,
                        false);
                }

                string? workingDirectory = TryGetStringArg(args, "workingDirectory", "working_directory", "cwd", "directory");
                if (!_sessionPolicy.IsTerminalDirectoryAllowed(workingDirectory))
                {
                    return new FunctionResult(toolName,
                        $"Working directory '{workingDirectory}' is outside the allowed terminal sandbox.",
                        FunctionResultSetContentModes.Passthrough,
                        false);
                }

                return await tool.RemoteTool.CallAsync(args, progress, serializer, fillContent, ct);
            }
        };

        return new Tool(function)
        {
            RemoteTool = remote,
            Strict = tool.Strict,
            VendorExtensions = tool.VendorExtensions
        };
    }

    private static string? TryGetStringArg(Dictionary<string, object?>? args, params string[] keys)
    {
        if (args is null)
            return null;

        foreach (string key in keys)
        {
            if (args.TryGetValue(key, out object? value) && value is not null)
                return value.ToString();
        }

        return null;
    }

    private async Task ConfigureBuiltInServerAsync(MCPServer server, string serverName, Action<string>? log)
    {
        if (_sessionPolicy is null || server.McpClient is null)
            return;

        if (!serverName.Equals(BuiltInMcpServerCatalog.DesktopCommanderServerName, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await server.McpClient.CallToolAsync("set_config_value", new Dictionary<string, object?>
            {
                ["key"] = "allowedDirectories",
                ["value"] = _sessionPolicy.GetAllowedFilesystemDirectories().ToArray()
            });

            await server.McpClient.CallToolAsync("set_config_value", new Dictionary<string, object?>
            {
                ["key"] = "blockedCommands",
                ["value"] = _sessionPolicy.BlockedCommands.ToArray()
            });
        }
        catch (Exception ex)
        {
            log?.Invoke($"  ! Failed to push built-in MCP policy to '{serverName}': {ex.Message}");
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
                Enabled = true,
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
                Enabled = true,
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
