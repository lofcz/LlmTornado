using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using LlmTornado.Cli.Blazor.Models;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Blazor.Controllers;

/// <summary>
/// ISettingsController implementation — wires Core managers to the settings UI.
/// </summary>
public sealed partial class ChatRuntimeController : ISettingsController
{
    // ─────────────────────────────────────────────
    // Working Directory
    // ─────────────────────────────────────────────

    public string GetWorkingDirectory()
        => _options.WorkingDirectory ?? Environment.CurrentDirectory;

    public async Task ChangeWorkingDirectoryAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        path = Path.GetFullPath(path);
        _options.WorkingDirectory = path;

        // Re-resolve paths that were not explicitly set at startup.
        // Update _options so that RefreshSkills/RefreshAgents/GetSkillsDirectory etc. stay consistent.
        if (!_skillsDirExplicit)
            _options.SkillsDirectory = Path.Combine(path, "skills");
        if (!_agentsDirExplicit)
            _options.AgentsDirectory = Path.Combine(path, "agents");
        if (!_mcpPathExplicit)
            _options.McpConfigPath = Path.Combine(path, "mcp.json");

        await ReconfigureSessionAsync(reloadSkills: true, reloadAgents: true, reloadMcp: true);
    }

    private async Task ReconfigureSessionAsync(bool reloadSkills, bool reloadAgents, bool reloadMcp)
    {
        string cwd = _options.WorkingDirectory ?? Environment.CurrentDirectory;
        _sessionPolicy = McpSessionPolicy.FromSettings(_settings, cwd);

        if (_skillManager is not null && reloadSkills)
        {
            _skillManager.LoadSkills(_options.SkillsDirectory!, _options.GlobalSkillsDirectory);
        }

        if (_mcpLoader is not null)
        {
            _mcpLoader.Configure(_settings, _sessionPolicy);
            if (reloadMcp)
                await _mcpLoader.LoadFromPathAsync(_options.McpConfigPath);
        }

        if (_agentManager is not null && reloadAgents)
        {
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string? globalDir = _options.GlobalAgentsDirectory
                ?? AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalDir, _options.AgentsDirectory!, cwd);

            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui?.SetAgents(uiAgents);
            Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);
        }

        if (_agentBuilder is not null)
        {
            _agentBuilder.WorkingDirectory = cwd;
            _runtime = _agentBuilder.Build(HandleRuntimeEvent);
        }
    }

    // ─────────────────────────────────────────────
    // MCP Servers
    // ─────────────────────────────────────────────

    public IReadOnlyList<McpServerStatus> GetMcpServerStatuses()
        => _mcpLoader?.ServerStatuses ?? [];

    public string GetMcpConfigPath()
        => _mcpLoader?.ConfigPath
           ?? McpConfigLoader.ResolveDefaultMcpConfigPath(_options.McpConfigPath);

    public string GetGlobalMcpConfigPath()
        => _mcpLoader?.GlobalConfigPath
           ?? McpConfigLoader.ResolveDefaultGlobalMcpConfigPath();

    public async Task OpenMcpConfigInEditorAsync()
    {
        string path = GetMcpConfigPath();
        await McpConfigLoader.CreateDefaultConfigIfMissingAsync(path);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // Editor launch failure is non-fatal
        }
    }

    public async Task ReloadMcpConfigAsync()
    {
        if (_mcpLoader is null) return;
        await ReconfigureSessionAsync(reloadSkills: false, reloadAgents: false, reloadMcp: true);
    }

    public McpConfig? GetMcpConfig(McpServerSource scope = McpServerSource.Local)
        => _mcpLoader?.ReadConfig(scope);

    public async Task SaveMcpConfigAsync(McpConfig config, McpServerSource scope = McpServerSource.Local)
    {
        if (_mcpLoader is null)
        {
            // Create a temporary loader just to save
            string path = scope == McpServerSource.Global
                ? McpConfigLoader.ResolveDefaultGlobalMcpConfigPath()
                : McpConfigLoader.ResolveDefaultMcpConfigPath(_options.McpConfigPath);
            string? dir = Path.GetDirectoryName(path);
            if (dir is not null) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            return;
        }

        await _mcpLoader.SaveConfigAsync(config, scope);
    }

    public async Task AddMcpServerAsync(McpServerEntry entry, McpServerSource scope = McpServerSource.Local)
    {
        // Remove any existing entry with the same name from both scopes to prevent duplicates
        await RemoveFromBothScopesAsync(entry.Name);

        McpConfig config = GetMcpConfig(scope) ?? new McpConfig { Servers = [] };
        config.Servers.Add(entry);
        await SaveMcpConfigAsync(config, scope);
        await ReloadMcpConfigAsync();
    }

    public async Task UpdateMcpServerAsync(string originalName, McpServerEntry entry, McpServerSource? newScope = null)
    {
        McpServerSource originalScope = FindServerScope(originalName);
        McpServerSource targetScope = newScope ?? originalScope;

        // Always remove the old entry from both scopes first to prevent duplicates
        await RemoveFromBothScopesAsync(originalName);

        // Write the updated entry to the target scope
        McpConfig config = GetMcpConfig(targetScope) ?? new McpConfig { Servers = [] };
        config.Servers.Add(entry);
        await SaveMcpConfigAsync(config, targetScope);

        await ReloadMcpConfigAsync();
    }

    public async Task RemoveMcpServerAsync(string serverName)
    {
        // Remove from both scopes to ensure no orphaned entries
        await RemoveFromBothScopesAsync(serverName);
        await ReloadMcpConfigAsync();
    }

    public async Task MoveMcpServerAsync(string serverName, McpServerSource targetScope)
    {
        McpServerSource currentScope = FindServerScope(serverName);

        // Find the entry before removing
        McpConfig? sourceConfig = GetMcpConfig(currentScope);
        McpServerEntry? entry = sourceConfig?.Servers.FirstOrDefault(s =>
            s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
        if (entry is null) return;

        // Remove from both scopes first to ensure clean state
        await RemoveFromBothScopesAsync(serverName);

        // Add to target scope
        McpConfig targetConfig = GetMcpConfig(targetScope) ?? new McpConfig { Servers = [] };
        targetConfig.Servers.Add(entry);
        await SaveMcpConfigAsync(targetConfig, targetScope);

        await ReloadMcpConfigAsync();
    }

    /// <summary>
    /// Remove a server entry by name from both local and global config files.
    /// </summary>
    private async Task RemoveFromBothScopesAsync(string serverName)
    {
        foreach (McpServerSource scope in new[] { McpServerSource.Local, McpServerSource.Global })
        {
            McpConfig? config = GetMcpConfig(scope);
            if (config is null) continue;

            int removed = config.Servers.RemoveAll(s =>
                s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                await SaveMcpConfigAsync(config, scope);
        }
    }

    private McpServerSource FindServerScope(string serverName)
    {
        // Check runtime statuses for the server's actual source
        McpServerStatus? status = _mcpLoader?.ServerStatuses
            .FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
        if (status is not null) return status.Source;

        // Fallback: check which config file has it
        McpConfig? local = GetMcpConfig(McpServerSource.Local);
        if (local?.Servers.Any(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase)) == true)
            return McpServerSource.Local;

        return McpServerSource.Global;
    }

    public Task<McpServerStatus> TestMcpConnectionAsync(McpServerEntry entry)
        => McpConfigLoader.TestConnectionAsync(entry);

    public async Task SetMcpServerEnabledAsync(string serverName, bool enabled)
    {
        if (enabled)
            _settings.DisabledMcpServers.Remove(serverName);
        else
            _settings.DisabledMcpServers.Add(serverName);

        SaveSettings(_settings);
        await ReconfigureSessionAsync(reloadSkills: false, reloadAgents: false, reloadMcp: true);
    }

    public IReadOnlyList<McpToolStatus> GetMcpToolStatuses(string serverName)
    {
        Dictionary<string, string> knownTools = new(StringComparer.OrdinalIgnoreCase);

        BuiltInMcpServerDefinition? builtIn = BuiltInMcpServerCatalog.GetDefinition(serverName, _sessionPolicy?.WorkingDirectory);
        if (builtIn is not null)
        {
            foreach (BuiltInMcpToolDefinition tool in builtIn.Tools)
                knownTools[tool.Name] = tool.Description;
        }

        if (_mcpLoader is not null)
        {
            foreach (var tool in _mcpLoader.AllTools)
            {
                if (_mcpLoader.ToolServerMap.TryGetValue(tool.ResolvedName, out string? mappedServer)
                    && mappedServer.Equals(serverName, StringComparison.OrdinalIgnoreCase))
                {
                    knownTools[tool.ResolvedName] = tool.ResolvedDescription;
                }
            }
        }

        if (_settings.DisabledMcpTools.TryGetValue(serverName, out HashSet<string>? disabledTools))
        {
            foreach (string toolName in disabledTools)
            {
                if (!knownTools.ContainsKey(toolName))
                    knownTools[toolName] = "Disabled MCP tool";
            }
        }

        HashSet<string> disabled = _settings.DisabledMcpTools.TryGetValue(serverName, out HashSet<string>? names)
            ? names
            : [];

        return [.. knownTools
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new McpToolStatus(x.Key, x.Value, !disabled.Contains(x.Key)))];
    }

    public async Task SetMcpToolEnabledAsync(string serverName, string toolName, bool enabled)
    {
        if (!_settings.DisabledMcpTools.TryGetValue(serverName, out HashSet<string>? disabledTools))
        {
            disabledTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _settings.DisabledMcpTools[serverName] = disabledTools;
        }

        if (enabled)
            disabledTools.Remove(toolName);
        else
            disabledTools.Add(toolName);

        if (disabledTools.Count == 0)
            _settings.DisabledMcpTools.Remove(serverName);

        SaveSettings(_settings);
        await ReconfigureSessionAsync(reloadSkills: false, reloadAgents: false, reloadMcp: true);
    }

    public McpSandboxSettings GetMcpSandboxSettings()
    {
        return new McpSandboxSettings(
            [.. _settings.FilesystemWhitelist.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)],
            [.. _settings.TerminalDirectoryWhitelist.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)],
            [.. _settings.AllowedCommands.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)],
            [.. _settings.BlockedCommands.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)]);
    }

    public async Task UpdateMcpSandboxSettingsAsync(McpSandboxSettings settings)
    {
        _settings.FilesystemWhitelist = [.. settings.FilesystemWhitelist.Where(x => !string.IsNullOrWhiteSpace(x))];
        _settings.TerminalDirectoryWhitelist = [.. settings.TerminalDirectoryWhitelist.Where(x => !string.IsNullOrWhiteSpace(x))];
        _settings.AllowedCommands = [.. settings.AllowedCommands.Where(x => !string.IsNullOrWhiteSpace(x))];
        _settings.BlockedCommands = [.. settings.BlockedCommands.Where(x => !string.IsNullOrWhiteSpace(x))];

        SaveSettings(_settings);
        await ReconfigureSessionAsync(reloadSkills: false, reloadAgents: false, reloadMcp: true);
    }

    // ─────────────────────────────────────────────
    // Skills
    // ─────────────────────────────────────────────

    public List<Skill> GetAllSkills()
        => _skillManager?.GetAllSkills() ?? [];

    public void SetSkillEnabled(string name, bool enabled)
    {
        if (_skillManager is null) return;

        if (enabled)
            _skillManager.EnableSkill(name);
        else
            _skillManager.DisableSkill(name);

        // Rebuild agent tools to reflect changes
        if (_agentBuilder is not null)
            _runtime = _agentBuilder.RebuildForSkillChange(HandleRuntimeEvent);
    }

    public string GetSkillsDirectory()
        => _options.SkillsDirectory ?? Path.GetFullPath("skills");

    public string GetGlobalSkillsDirectory()
        => _options.GlobalSkillsDirectory ?? SkillLoader.ResolveGlobalSkillsDirectory();

    public string? ReadSkillFile(string skillName)
    {
        Skill? skill = _skillManager?.GetSkill(skillName);
        if (skill is null) return null;

        try
        {
            return File.Exists(skill.SkillMdPath)
                ? File.ReadAllText(skill.SkillMdPath)
                : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveSkillFileAsync(string skillName, string content)
    {
        Skill? skill = _skillManager?.GetSkill(skillName);
        if (skill is null) return;

        await File.WriteAllTextAsync(skill.SkillMdPath, content);
    }

    public void RefreshSkills()
    {
        if (_skillManager is null) return;

        string skillsDir = _options.SkillsDirectory ?? Path.GetFullPath("skills");
        string? globalSkillsDir = _options.GlobalSkillsDirectory
            ?? SkillLoader.ResolveGlobalSkillsDirectory();
        _skillManager.LoadSkills(skillsDir, globalSkillsDir);

        // Rebuild agent tools
        if (_agentBuilder is not null)
            _runtime = _agentBuilder.RebuildForSkillChange(HandleRuntimeEvent);
    }

    public async Task ImportSkillAsync(string fileName, Stream content, SkillSource scope = SkillSource.Project)
    {
        string skillsDir = scope == SkillSource.Global
            ? (_options.GlobalSkillsDirectory ?? SkillLoader.ResolveGlobalSkillsDirectory())
            : (_options.SkillsDirectory ?? Path.GetFullPath("skills"));
        Directory.CreateDirectory(skillsDir);

        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            // Extract zip into skills directory
            string tempPath = Path.GetTempFileName();
            try
            {
                await using (FileStream fs = new(tempPath, FileMode.Create))
                {
                    await content.CopyToAsync(fs);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory(tempPath, skillsDir, overwriteFiles: true);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
        else
        {
            // Treat as SKILL.md — create a folder named after the file (minus extension)
            string skillName = Path.GetFileNameWithoutExtension(fileName);
            string skillFolder = Path.Combine(skillsDir, skillName);
            Directory.CreateDirectory(skillFolder);

            string destPath = Path.Combine(skillFolder, "SKILL.md");
            await using FileStream fs = new(destPath, FileMode.Create);
            await content.CopyToAsync(fs);
        }

        RefreshSkills();
    }

    public async Task MoveSkillAsync(string skillName, SkillSource targetScope)
    {
        Skill? skill = _skillManager?.GetSkill(skillName);
        if (skill is null || skill.Source == targetScope) return;

        string targetDir = targetScope == SkillSource.Global
            ? (_options.GlobalSkillsDirectory ?? SkillLoader.ResolveGlobalSkillsDirectory())
            : (_options.SkillsDirectory ?? Path.GetFullPath("skills"));

        string targetSkillDir = Path.Combine(targetDir, skillName);
        Directory.CreateDirectory(targetDir);

        // Copy entire skill directory to target
        CopyDirectory(skill.DirectoryPath, targetSkillDir);

        // Remove source directory
        if (Directory.Exists(skill.DirectoryPath))
            Directory.Delete(skill.DirectoryPath, recursive: true);

        RefreshSkills();
        await Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string destDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    // ─────────────────────────────────────────────
    // Agents
    // ─────────────────────────────────────────────

    public List<AgentDefinition> GetAllAgentDefinitions()
        => _agentManager?.GetAllPersonas() ?? [];

    public string? GetActiveAgentName()
        => _agentManager?.ActivePersonaName;

    public async Task CreateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null,
        AgentSource scope = AgentSource.Custom)
    {
        if (_agentManager is null) return;

        string agentsDir = scope == AgentSource.Global
            ? (_options.GlobalAgentsDirectory ?? AgentDefinitionLoader.ResolveGlobalAgentsDirectory())
            : (_options.AgentsDirectory ?? Path.GetFullPath("agents"));

        _agentManager.CreateAgent(agentsDir, name, description, instructions,
            enabledSkills, disabledSkills, enabledTools, disabledTools);

        // Reload to pick up the new agent file
        RefreshAgents();
        await Task.CompletedTask;
    }

    public async Task UpdateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null)
    {
        if (_agentManager is null) return;

        _agentManager.UpdateAgent(name, description, instructions,
            enabledSkills, disabledSkills, enabledTools, disabledTools);

        RefreshAgents();
        await Task.CompletedTask;
    }

    public async Task DeleteAgentAsync(string name)
    {
        if (_agentManager is null) return;

        _agentManager.DeleteAgent(name);
        RefreshAgents();
        await Task.CompletedTask;
    }

    public string GetAgentsDirectory()
        => _options.AgentsDirectory ?? Path.GetFullPath("agents");

    public string GetGlobalAgentsDirectory()
        => _options.GlobalAgentsDirectory ?? AgentDefinitionLoader.ResolveGlobalAgentsDirectory();

    public async Task MoveAgentAsync(string agentName, AgentSource targetScope)
    {
        AgentDefinition? agent = _agentManager?.GetPersona(agentName);
        if (agent is null) return;
        if (agent.Source == AgentSource.BuiltIn) return; // can't move built-ins
        if ((agent.Source == AgentSource.Custom && targetScope == AgentSource.Custom)
            || (agent.Source == AgentSource.Global && targetScope == AgentSource.Global))
            return; // already in target scope

        string targetDir = targetScope == AgentSource.Global
            ? GetGlobalAgentsDirectory()
            : GetAgentsDirectory();

        Directory.CreateDirectory(targetDir);

        string fileName = Path.GetFileName(agent.FilePath);
        string targetPath = Path.Combine(targetDir, fileName);

        File.Copy(agent.FilePath, targetPath, overwrite: true);

        if (File.Exists(agent.FilePath))
            File.Delete(agent.FilePath);

        RefreshAgents();
        await Task.CompletedTask;
    }

    public void RefreshAgents()
    {
        if (_agentManager is null) return;

        string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
        string agentsDir = _options.AgentsDirectory ?? Path.GetFullPath("agents");
        string cwd = _options.WorkingDirectory ?? Environment.CurrentDirectory;
        string? globalDir = _options.GlobalAgentsDirectory
            ?? AgentDefinitionLoader.ResolveGlobalAgentsDirectory();

        _agentManager.LoadAll(builtInDir, globalDir, agentsDir, cwd);

        // Update the UI agent dropdown
        List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
            .Where(a => a.IsPersona)
            .Select(MapAgent)
            .ToList();
        Ui?.SetAgents(uiAgents);
        Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);

        // Rebuild agent to reflect any persona changes
        if (_agentBuilder is not null)
            _runtime = _agentBuilder.RebuildForAgentChange(HandleRuntimeEvent);
    }

    public List<ToolInfo> GetAvailableTools()
    {
        Dictionary<string, ToolInfo> tools = new(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, string>? toolServerMap = _mcpLoader?.ToolServerMap;

        // MCP tools — have both name and description
        if (_mcpLoader is not null)
        {
            foreach (var tool in _mcpLoader.AllTools)
            {
                string name = tool.ResolvedName;
                if (!string.IsNullOrEmpty(name) && !tools.ContainsKey(name))
                {
                    string? serverName = null;
                    if (toolServerMap is not null)
                        toolServerMap.TryGetValue(name, out serverName);
                    tools[name] = new ToolInfo(name, tool.ResolvedDescription ?? "", serverName);
                }
            }
        }

        // Skill script tools — names only (description from AllowedTools list)
        if (_skillManager is not null)
        {
            foreach (Skill skill in _skillManager.GetAllSkills())
            {
                foreach (string toolName in skill.AllowedTools)
                {
                    if (!tools.ContainsKey(toolName))
                        tools[toolName] = new ToolInfo(toolName, $"Allowed by skill '{skill.Name}'");
                }
            }
        }

        return tools.Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public AgentDependencyReport AnalyzeAgentDependencies(string agentName)
    {
        AgentDefinition? agent = _agentManager?.GetPersona(agentName);
        if (agent is null || !agent.HasCapabilityCuration)
            return new AgentDependencyReport();

        List<string> localOnlySkills = [];
        List<string> missingSkills = [];
        List<string> localOnlyTools = [];
        List<string> missingTools = [];

        // Build skill lookup: name → source
        Dictionary<string, SkillSource> skillSources = new(StringComparer.OrdinalIgnoreCase);
        if (_skillManager is not null)
        {
            foreach (Skill skill in _skillManager.GetAllSkills())
                skillSources[skill.Name] = skill.Source;
        }

        // Check referenced skills (from both enabled and disabled lists)
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string skillName in agent.EnabledSkills.Concat(agent.DisabledSkills))
        {
            if (!seen.Add(skillName)) continue;
            if (skillSources.TryGetValue(skillName, out SkillSource source))
            {
                if (source == SkillSource.Project)
                    localOnlySkills.Add(skillName);
            }
            else
            {
                missingSkills.Add(skillName);
            }
        }

        // Build tool lookup from McpConfigLoader's ToolSourceMap
        IReadOnlyDictionary<string, McpServerSource>? toolSources = _mcpLoader?.ToolSourceMap;

        // Also collect skill-owned tool names
        HashSet<string> skillToolNames = new(StringComparer.OrdinalIgnoreCase);
        if (_skillManager is not null)
        {
            foreach (Skill skill in _skillManager.GetAllSkills())
                foreach (string t in skill.AllowedTools)
                    skillToolNames.Add(t);
        }

        // Check referenced tools
        seen.Clear();
        foreach (string toolName in agent.EnabledTools.Concat(agent.DisabledTools).Concat(agent.AutoApproveTools))
        {
            if (!seen.Add(toolName)) continue;

            if (toolSources is not null && toolSources.TryGetValue(toolName, out McpServerSource mcpSource))
            {
                if (mcpSource == McpServerSource.Local)
                    localOnlyTools.Add(toolName);
            }
            else if (!skillToolNames.Contains(toolName))
            {
                missingTools.Add(toolName);
            }
        }

        return new AgentDependencyReport
        {
            LocalOnlySkills = localOnlySkills,
            MissingSkills = missingSkills,
            LocalOnlyTools = localOnlyTools,
            MissingTools = missingTools,
        };
    }
}
