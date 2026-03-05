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

        // 1. Reload skills
        if (_skillManager is not null)
        {
            _skillManager.LoadSkills(_options.SkillsDirectory!, _options.GlobalSkillsDirectory);
        }

        // 2. Reload MCP servers from new config path
        if (_mcpLoader is not null)
        {
            await _mcpLoader.LoadFromPathAsync(_options.McpConfigPath);
        }

        // 3. Reload agents
        if (_agentManager is not null)
        {
            string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
            string? globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
            _agentManager.LoadAll(builtInDir, globalDir, _options.AgentsDirectory!, path);

            List<ChatUiAgent> uiAgents = _agentManager.GetAllPersonas()
                .Where(a => a.IsPersona)
                .Select(MapAgent)
                .ToList();
            Ui?.SetAgents(uiAgents);
            Ui?.SetSelectedAgent(_agentManager.ActivePersonaName);
        }

        // 4. Rebuild the agent runtime with updated tools and system prompt
        if (_agentBuilder is not null)
        {
            _agentBuilder.WorkingDirectory = path;
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

        await _mcpLoader.ReloadAsync();

        // Rebuild agent tools to pick up any changes
        if (_agentBuilder is not null)
            _runtime = _agentBuilder.RebuildForSkillChange(HandleRuntimeEvent);
    }

    public McpConfig? GetMcpConfig()
        => _mcpLoader?.ReadConfig();

    public async Task SaveMcpConfigAsync(McpConfig config)
    {
        if (_mcpLoader is null)
        {
            // Create a temporary loader just to save
            McpConfigLoader loader = new();
            string path = McpConfigLoader.ResolveDefaultMcpConfigPath(_options.McpConfigPath);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            string? dir = Path.GetDirectoryName(path);
            if (dir is not null) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, json);
            return;
        }

        await _mcpLoader.SaveConfigAsync(config);
    }

    public async Task AddMcpServerAsync(McpServerEntry entry)
    {
        McpConfig config = GetMcpConfig() ?? new McpConfig { Servers = [] };
        config.Servers.Add(entry);
        await SaveMcpConfigAsync(config);
        await ReloadMcpConfigAsync();
    }

    public async Task UpdateMcpServerAsync(string originalName, McpServerEntry entry)
    {
        McpConfig? config = GetMcpConfig();
        if (config is null) return;

        int idx = config.Servers.FindIndex(s =>
            s.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;

        config.Servers[idx] = entry;
        await SaveMcpConfigAsync(config);
        await ReloadMcpConfigAsync();
    }

    public async Task RemoveMcpServerAsync(string serverName)
    {
        McpConfig? config = GetMcpConfig();
        if (config is null) return;

        config.Servers.RemoveAll(s =>
            s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
        await SaveMcpConfigAsync(config);
        await ReloadMcpConfigAsync();
    }

    public Task<McpServerStatus> TestMcpConnectionAsync(McpServerEntry entry)
        => McpConfigLoader.TestConnectionAsync(entry);

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
        _skillManager.LoadSkills(skillsDir, _options.GlobalSkillsDirectory);

        // Rebuild agent tools
        if (_agentBuilder is not null)
            _runtime = _agentBuilder.RebuildForSkillChange(HandleRuntimeEvent);
    }

    public async Task ImportSkillAsync(string fileName, Stream content)
    {
        string skillsDir = _options.SkillsDirectory ?? Path.GetFullPath("skills");
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

    // ─────────────────────────────────────────────
    // Agents
    // ─────────────────────────────────────────────

    public List<AgentDefinition> GetAllAgentDefinitions()
        => _agentManager?.GetAllPersonas() ?? [];

    public string? GetActiveAgentName()
        => _agentManager?.ActivePersonaName;

    public async Task CreateAgentAsync(string name, string description, string instructions,
        List<string>? enabledSkills = null, List<string>? disabledSkills = null,
        List<string>? enabledTools = null, List<string>? disabledTools = null)
    {
        if (_agentManager is null) return;

        string agentsDir = _options.AgentsDirectory ?? Path.GetFullPath("agents");
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

    public void RefreshAgents()
    {
        if (_agentManager is null) return;

        string builtInDir = Path.Combine(AppContext.BaseDirectory, "Agents", "built-in");
        string agentsDir = _options.AgentsDirectory ?? Path.GetFullPath("agents");
        string cwd = _options.WorkingDirectory ?? Environment.CurrentDirectory;
        string? globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();

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

        // MCP tools — have both name and description
        if (_mcpLoader is not null)
        {
            foreach (var tool in _mcpLoader.AllTools)
            {
                string name = tool.ResolvedName;
                if (!string.IsNullOrEmpty(name) && !tools.ContainsKey(name))
                    tools[name] = new ToolInfo(name, tool.ResolvedDescription ?? "");
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
}
