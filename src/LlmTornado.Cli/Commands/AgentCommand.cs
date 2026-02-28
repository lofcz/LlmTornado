using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class AgentCommand : ICliCommand
{
    public string Name => "agent";
    public string Description => "Manage agent personas (list, set, clear, info, project)";
    public string Usage => "/agent [list | set <name> | clear | info <name> | project [on|off]]";

    private readonly AgentDefinitionManager _agentManager;
    private readonly SkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly AgentSettings _settings;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public AgentCommand(
        AgentDefinitionManager agentManager,
        SkillManager skillManager,
        CliAgentBuilder builder,
        AgentSettings settings,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _agentManager = agentManager;
        _skillManager = skillManager;
        _builder = builder;
        _settings = settings;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowStatus();
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                ListAgents();
                break;

            case "set" when args.Length >= 2:
                SetAgent(args[1]);
                break;

            case "clear":
                ClearAgent();
                break;

            case "info" when args.Length >= 2:
                ShowInfo(args[1]);
                break;

            case "project":
                HandleProject(args.Length >= 2 ? args[1] : null);
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }

    private void ShowStatus()
    {
        string activeName = _agentManager.ActivePersonaName ?? "default (none)";
        AgentDefinition? active = _agentManager.GetActivePersona();
        AgentDefinition? project = _agentManager.GetProjectContext();

        ConsoleRenderer.WriteInfo($"Active agent: {activeName}");

        if (active is not null)
        {
            ConsoleRenderer.WriteInfo($"  Source:  {active.Source}");
            if (active.EnabledSkills.Count > 0)
                ConsoleRenderer.WriteInfo($"  Skills:  {string.Join(", ", active.EnabledSkills)}");
            if (active.DisabledTools.Count > 0)
                ConsoleRenderer.WriteInfo($"  Blocked: {string.Join(", ", active.DisabledTools)}");
        }

        ConsoleRenderer.WriteInfo($"Project AGENTS.md: {(project is not null ? "detected" : "not found")}");
        ConsoleRenderer.WriteInfo($"Total personas: {_agentManager.GetAllPersonas().Count}");
    }

    private void ListAgents()
    {
        List<AgentDefinition> agents = _agentManager.GetAllPersonas();
        if (agents.Count == 0)
        {
            ConsoleRenderer.WriteInfo("No agent personas found.");
            return;
        }

        foreach (AgentDefinition agent in agents)
        {
            string marker = agent.Name == _agentManager.ActivePersonaName ? "→ " : "  ";
            string sourceTag = agent.Source switch
            {
                AgentSource.BuiltIn => "[built-in]",
                AgentSource.Custom => "[custom]",
                _ => ""
            };

            string curation = "";
            if (agent.HasCapabilityCuration)
            {
                List<string> parts = [];
                if (agent.EnabledSkills.Count > 0)
                    parts.Add($"{agent.EnabledSkills.Count} skills");
                if (agent.DisabledTools.Count > 0)
                    parts.Add($"{agent.DisabledTools.Count} blocked tools");
                if (parts.Count > 0)
                    curation = $" ({string.Join(", ", parts)})";
            }

            ConsoleRenderer.WriteInfo(
                $"{marker}{agent.Name,-20} {sourceTag,-12} {agent.Description}{curation}");
        }
    }

    private void SetAgent(string name)
    {
        AgentDefinition? selected = _agentManager.SetActivePersona(name);
        if (selected is null)
        {
            ConsoleRenderer.WriteError($"Agent '{name}' not found. Use /agent list to see available agents.");
            return;
        }

        _builder.RebuildForAgentChange(_runtimeEventHandler);

        List<Skill> enabledSkills = _skillManager.GetEnabledSkills();
        List<Skill> allSkills = _skillManager.GetAllSkills();
        ConsoleRenderer.WriteSuccess(
            $"Activated agent: {selected.Name} ({enabledSkills.Count}/{allSkills.Count} skills enabled)");

        if (selected.HasCapabilityCuration)
        {
            if (selected.EnabledSkills.Count > 0)
                ConsoleRenderer.WriteInfo($"  Enabled skills: {string.Join(", ", selected.EnabledSkills)}");
            if (selected.DisabledSkills.Count > 0)
                ConsoleRenderer.WriteInfo($"  Disabled skills: {string.Join(", ", selected.DisabledSkills)}");
            if (selected.DisabledTools.Count > 0)
                ConsoleRenderer.WriteInfo($"  Blocked tools: {string.Join(", ", selected.DisabledTools)}");
            if (selected.AutoApproveTools.Count > 0)
                ConsoleRenderer.WriteInfo($"  Auto-approved: {string.Join(", ", selected.AutoApproveTools)}");
        }
    }

    private void ClearAgent()
    {
        _agentManager.ClearActivePersona();
        _builder.RebuildForAgentChange(_runtimeEventHandler);

        List<Skill> nowEnabled = _skillManager.GetEnabledSkills();
        List<Skill> nowAll = _skillManager.GetAllSkills();
        ConsoleRenderer.WriteSuccess(
            $"Agent cleared. All capabilities restored ({nowEnabled.Count}/{nowAll.Count} skills enabled).");
    }

    private void ShowInfo(string name)
    {
        AgentDefinition? info = _agentManager.GetPersona(name);
        if (info is null)
        {
            ConsoleRenderer.WriteError($"Agent '{name}' not found.");
            return;
        }

        ConsoleRenderer.WriteInfo($"  Name:            {info.Name}");
        ConsoleRenderer.WriteInfo($"  Description:     {info.Description}");
        ConsoleRenderer.WriteInfo($"  Source:           {info.Source}");
        ConsoleRenderer.WriteInfo($"  File:            {info.FilePath}");
        ConsoleRenderer.WriteInfo($"  Active:          {(info.Name == _agentManager.ActivePersonaName ? "yes" : "no")}");
        ConsoleRenderer.WriteInfo($"  Has curation:    {info.HasCapabilityCuration}");

        if (info.EnabledSkills.Count > 0)
            ConsoleRenderer.WriteInfo($"  Enabled skills:  {string.Join(", ", info.EnabledSkills)}");
        if (info.DisabledSkills.Count > 0)
            ConsoleRenderer.WriteInfo($"  Disabled skills: {string.Join(", ", info.DisabledSkills)}");
        if (info.EnabledTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Enabled tools:   {string.Join(", ", info.EnabledTools)}");
        if (info.DisabledTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Disabled tools:  {string.Join(", ", info.DisabledTools)}");
        if (info.AutoApproveTools.Count > 0)
            ConsoleRenderer.WriteInfo($"  Auto-approve:    {string.Join(", ", info.AutoApproveTools)}");

        if (!string.IsNullOrWhiteSpace(info.Instructions))
        {
            ConsoleRenderer.WriteInfo("  Instructions:");
            string[] lines = info.Instructions.Split('\n');
            int previewLines = Math.Min(10, lines.Length);
            for (int i = 0; i < previewLines; i++)
                ConsoleRenderer.WriteInfo($"    {lines[i].TrimEnd()}");
            if (lines.Length > previewLines)
                ConsoleRenderer.WriteInfo($"    ... ({lines.Length - previewLines} more lines)");
        }
    }

    private void HandleProject(string? subArg)
    {
        if (subArg is not null)
        {
            bool enable = subArg.ToLowerInvariant() switch
            {
                "on" or "true" or "1" => true,
                "off" or "false" or "0" => false,
                _ => _settings.ProjectAgentsEnabled
            };

            if (enable != _settings.ProjectAgentsEnabled)
            {
                _settings.ProjectAgentsEnabled = enable;
                CliStorage.SaveJson(CliStorage.SettingsPath, _settings);

                _agentManager.RefreshProjectContext(Environment.CurrentDirectory);
                _builder.RebuildForAgentChange(_runtimeEventHandler);

                ConsoleRenderer.WriteSuccess(
                    $"Project AGENTS.md: {(enable ? "enabled" : "disabled")}");
            }
            else
            {
                ConsoleRenderer.WriteInfo(
                    $"Project AGENTS.md already {(enable ? "enabled" : "disabled")}.");
            }
        }
        else
        {
            AgentDefinition? project = _agentManager.GetProjectContext();
            ConsoleRenderer.WriteInfo(
                $"Project AGENTS.md: {(_settings.ProjectAgentsEnabled ? "enabled" : "disabled")}");
            if (project is not null)
                ConsoleRenderer.WriteInfo($"  Detected: {project.FilePath}");
            else
                ConsoleRenderer.WriteInfo("  No AGENTS.md found in current directory hierarchy.");
        }
    }
}
