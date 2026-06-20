using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Authoring;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class AgentCommand : ICliCommand
{
    public string Name => "agent";
    public string Description => "Manage agent personas (list, set, clear, info, project, create, edit, remove)";
    public string Usage => "/agent [list | set <name> | clear | info <name> | project [on|off] | create | edit <name> | remove <name>]";

    private readonly AgentDefinitionManager _agentManager;
    private readonly SkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly AgentSettings _settings;
    private readonly ProviderDetectionResult _providers;
    private readonly IUserInteractionHandler _interaction;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public AgentCommand(
        AgentDefinitionManager agentManager,
        SkillManager skillManager,
        CliAgentBuilder builder,
        AgentSettings settings,
        ProviderDetectionResult providers,
        IUserInteractionHandler interaction,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _agentManager = agentManager;
        _skillManager = skillManager;
        _builder = builder;
        _settings = settings;
        _providers = providers;
        _interaction = interaction;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowStatus();
            return true;
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

            case "create":
                await CreateAgentAsync();
                break;

            case "edit" when args.Length >= 2:
                await EditAgentAsync(args[1]);
                break;

            case "remove" or "delete" when args.Length >= 2:
                await RemoveAgentAsync(args[1]);
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
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

    private async Task CreateAgentAsync()
    {
        string customDir = AgentDefinitionLoader.ResolveAgentsDirectory(_settings.AgentsDirectory);
        string globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();

        AskQuestionsInteractionRequest request = new()
        {
            Title = "Create a new agent persona",
            Message = "Answer a few questions; the model will draft the persona's instructions from your answers.",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "name", Prompt = "Agent name", Required = true,
                    Description = "Used as the persona name and the <name>.md filename.",
                },
                new InteractiveQuestionDefinition
                {
                    Key = "description", Prompt = "One-line description", Required = true,
                },
                WizardSupport.SaveLocationQuestion(customDir, globalDir),
                new InteractiveQuestionDefinition
                {
                    Key = "brief", Prompt = "Describe the persona's role and behavior", Required = true,
                    Description = "A short brief the model will expand into the system prompt.",
                },
                BuildSkillSelectQuestion("Select to whitelist the skills this persona may use; leave blank to allow all."),
                WizardSupport.ToolSelectQuestion("disabled-tools", "Tools to block (optional)",
                    "Select tools this persona should not use. Pick numbers, add a custom id, or leave blank for none.",
                    _builder.Agent.ToolList.Keys),
            ],
        };

        AskQuestionsInteractionResponse answers = await _interaction.AskQuestionsAsync(request);

        string name = answers.Text("name");
        string description = answers.Text("description");
        string brief = answers.Text("brief");
        string location = answers.Choice("location", WizardSupport.LocationProject);
        List<string> enabledSkills = answers.Selected("skills");
        List<string> disabledTools = answers.Selected("disabled-tools");

        if (string.IsNullOrWhiteSpace(name))
        {
            ConsoleRenderer.WriteError("Agent name is required.");
            return;
        }
        if (_agentManager.GetPersona(name) is not null)
        {
            ConsoleRenderer.WriteError($"An agent named '{name}' already exists. Use /agent edit {name} instead.");
            return;
        }

        string agentsDir = location == WizardSupport.LocationGlobal ? globalDir : customDir;

        ConsoleRenderer.WriteInfo("Drafting persona instructions with the authoring assistant...");
        string body = await AuthoringAssistant.DraftAsync(
            _providers.Api, _providers.ActiveModel, AuthoringAssistant.AgentAuthorPrompt,
            BuildAgentBrief(name, description, brief, enabledSkills, disabledTools));

        try
        {
            _agentManager.CreateAgent(agentsDir, name, description, body,
                enabledSkills: enabledSkills.Count > 0 ? enabledSkills : null,
                disabledTools: disabledTools.Count > 0 ? disabledTools : null);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to write agent: {ex.Message}");
            return;
        }

        ReloadAgents();
        ConsoleRenderer.WriteSuccess($"Created agent '{name}' ({location}). Activate it with /agent set {name}.");
        PreviewAgent(name, body);
    }

    private async Task EditAgentAsync(string name)
    {
        AgentDefinition? existing = _agentManager.GetPersona(name);
        if (existing is null)
        {
            ConsoleRenderer.WriteError($"Agent '{name}' not found. Use /agent list to see available agents.");
            return;
        }
        if (existing.Source is AgentSource.BuiltIn)
        {
            ConsoleRenderer.WriteError($"'{name}' is a built-in agent and cannot be edited. Use /agent create to make your own.");
            return;
        }

        AskQuestionsInteractionRequest request = new()
        {
            Title = $"Edit agent '{existing.Name}'",
            Message = "Leave a field blank to keep its current value.",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "description", Prompt = "Description", Required = false,
                    Description = $"Current: {existing.Description}",
                },
                BuildSkillSelectQuestion(
                    $"Current: {(existing.EnabledSkills.Count > 0 ? string.Join(", ", existing.EnabledSkills) : "all")}. Select to replace the whitelist, or leave blank to keep."),
                WizardSupport.ToolSelectQuestion("disabled-tools", "Tools to block",
                    $"Current: {(existing.DisabledTools.Count > 0 ? string.Join(' ', existing.DisabledTools) : "none")}. Select to replace, or leave blank to keep.",
                    _builder.Agent.ToolList.Keys),
                new InteractiveQuestionDefinition
                {
                    Key = "brief", Prompt = "Re-draft instructions from a new brief? (optional)", Required = false,
                    Description = "Describe changes to regenerate the persona. Leave blank to keep current instructions.",
                },
            ],
        };

        AskQuestionsInteractionResponse answers = await _interaction.AskQuestionsAsync(request);

        string description = answers.Text("description", existing.Description);
        List<string> selectedSkills = answers.Selected("skills");
        List<string> enabledSkills = selectedSkills.Count > 0 ? selectedSkills : existing.EnabledSkills;
        List<string> selectedDisabledTools = answers.Selected("disabled-tools");
        List<string> disabledTools = selectedDisabledTools.Count > 0 ? selectedDisabledTools : existing.DisabledTools;
        string brief = answers.Text("brief");

        string body = existing.Instructions;
        if (!string.IsNullOrWhiteSpace(brief))
        {
            ConsoleRenderer.WriteInfo("Re-drafting persona instructions with the authoring assistant...");
            string context =
                BuildAgentBrief(existing.Name, description, brief, enabledSkills, disabledTools) +
                $"\n\nExisting instructions:\n{existing.Instructions}";
            body = await AuthoringAssistant.DraftAsync(
                _providers.Api, _providers.ActiveModel, AuthoringAssistant.AgentAuthorPrompt, context);
        }

        bool updated = _agentManager.UpdateAgent(existing.Name, description, body,
            enabledSkills: enabledSkills.Count > 0 ? enabledSkills : null,
            disabledSkills: existing.DisabledSkills.Count > 0 ? existing.DisabledSkills : null,
            enabledTools: existing.EnabledTools.Count > 0 ? existing.EnabledTools : null,
            disabledTools: disabledTools.Count > 0 ? disabledTools : null);

        if (!updated)
        {
            ConsoleRenderer.WriteError($"Failed to update agent '{name}'.");
            return;
        }

        ReloadAgents();
        ConsoleRenderer.WriteSuccess($"Updated agent '{existing.Name}'.");
        PreviewAgent(existing.Name, body);
    }

    private async Task RemoveAgentAsync(string name)
    {
        AgentDefinition? existing = _agentManager.GetPersona(name);
        if (existing is null)
        {
            ConsoleRenderer.WriteError($"Agent '{name}' not found. Use /agent list to see available agents.");
            return;
        }
        if (existing.Source is AgentSource.BuiltIn)
        {
            ConsoleRenderer.WriteError($"'{name}' is a built-in agent and cannot be removed.");
            return;
        }

        bool confirmed = await _interaction.ConfirmAsync(
            $"Remove agent '{existing.Name}'",
            $"Delete '{existing.Name}' ({existing.FilePath})? This cannot be undone.");
        if (!confirmed)
        {
            ConsoleRenderer.WriteInfo("Cancelled. Nothing was deleted.");
            return;
        }

        if (!_agentManager.DeleteAgent(existing.Name))
        {
            ConsoleRenderer.WriteError($"Failed to remove agent '{name}'.");
            return;
        }

        ReloadAgents();
        ConsoleRenderer.WriteSuccess($"Removed agent '{existing.Name}'.");
    }

    private InteractiveQuestionDefinition BuildSkillSelectQuestion(string description)
    {
        IEnumerable<InteractiveQuestionOption> options = _skillManager.GetAllSkills()
            .Select(s => new InteractiveQuestionOption { Value = s.Name, Label = s.Name, Description = s.Description });
        return WizardSupport.MultiSelectQuestion("skills", "Skills this persona may use (optional)",
            description, "no skills available — leave blank to allow all", options);
    }

    private static string BuildAgentBrief(string name, string description, string brief,
        List<string> enabledSkills, List<string> disabledTools) =>
        $"Agent name: {name}\nDescription: {description}\n" +
        $"Enabled skills: {(enabledSkills.Count > 0 ? string.Join(", ", enabledSkills) : "all")}\n" +
        $"Blocked tools: {(disabledTools.Count > 0 ? string.Join(", ", disabledTools) : "none")}\n" +
        $"Brief: {brief}";

    private void ReloadAgents()
    {
        string customDir = AgentDefinitionLoader.ResolveAgentsDirectory(_settings.AgentsDirectory);
        string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
        string globalDir = AgentDefinitionLoader.ResolveGlobalAgentsDirectory();
        _agentManager.LoadAll(builtInDir, globalDir, customDir, Environment.CurrentDirectory);
        _builder.RebuildForAgentChange(_runtimeEventHandler);
    }

    private static void PreviewAgent(string name, string body)
    {
        ConsoleRenderer.WriteInfo("  Preview:");
        string[] lines = body.Split('\n');
        int preview = Math.Min(12, lines.Length);
        for (int i = 0; i < preview; i++)
            ConsoleRenderer.WriteInfo($"    {lines[i].TrimEnd()}");
        if (lines.Length > preview)
            ConsoleRenderer.WriteInfo($"    ... ({lines.Length - preview} more lines)");
        ConsoleRenderer.WriteInfo($"  Refine it with /agent edit {name}, or open the .md file in your editor.");
    }
}
