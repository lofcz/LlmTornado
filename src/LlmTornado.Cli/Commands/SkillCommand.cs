using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Authoring;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class SkillCommand : ICliCommand
{
    public string Name => "skill";
    public string Description => "Manage skills (list, enable, disable, info, create, edit, remove)";
    public string Usage => "/skill [list | enable <name> | disable <name> | info <name> | create | edit <name> | remove <name>]";

    private readonly SkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly AgentSettings _settings;
    private readonly ProviderDetectionResult _providers;
    private readonly IUserInteractionHandler _interaction;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public SkillCommand(
        SkillManager skillManager,
        CliAgentBuilder builder,
        AgentSettings settings,
        ProviderDetectionResult providers,
        IUserInteractionHandler interaction,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
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
            List<Skill> enabled = _skillManager.GetEnabledSkills();
            List<Skill> all = _skillManager.GetAllSkills();
            ConsoleRenderer.WriteInfo($"{enabled.Count}/{all.Count} skills enabled.");
            return true;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                List<Skill> skills = _skillManager.GetAllSkills();
                if (skills.Count == 0)
                {
                    string projectDir = SkillLoader.ResolveSkillsDirectory(_settings.SkillsDirectory);
                    string globalDir = SkillLoader.ResolveGlobalSkillsDirectory();
                    ConsoleRenderer.WriteInfo($"No skills found. Checked project: {projectDir} and global: {globalDir}.");
                    ConsoleRenderer.WriteInfo("Add skills there (folder-name/SKILL.md) or run /skill create.");
                    break;
                }
                foreach (Skill skill in skills)
                {
                    string status = skill.Enabled ? "✓" : "✗";
                    string activated = skill.Activated ? " [active in context]" : "";
                    ConsoleRenderer.WriteInfo($"  {status} {skill.Name,-25} {skill.Description}{activated}");
                }
                break;

            case "enable" when args.Length >= 2:
                if (_skillManager.EnableSkill(args[1]))
                {
                    _builder.RebuildForSkillChange(_runtimeEventHandler);
                    ConsoleRenderer.WriteSuccess($"Enabled skill: {args[1]}");
                }
                else
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                break;

            case "disable" when args.Length >= 2:
                if (_skillManager.DisableSkill(args[1]))
                {
                    _builder.RebuildForSkillChange(_runtimeEventHandler);
                    ConsoleRenderer.WriteSuccess($"Disabled skill: {args[1]}");
                }
                else
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                break;

            case "info" when args.Length >= 2:
                Skill? info = _skillManager.GetSkill(args[1]);
                if (info is null)
                {
                    ConsoleRenderer.WriteError($"Skill '{args[1]}' not found.");
                    break;
                }
                ConsoleRenderer.WriteInfo($"  Name:          {info.Name}");
                ConsoleRenderer.WriteInfo($"  Description:   {info.Description}");
                ConsoleRenderer.WriteInfo($"  Enabled:       {info.Enabled}");
                ConsoleRenderer.WriteInfo($"  Activated:     {info.Activated}");
                ConsoleRenderer.WriteInfo($"  License:       {info.License ?? "(none)"}");
                ConsoleRenderer.WriteInfo($"  Compatibility: {info.Compatibility ?? "(none)"}");
                ConsoleRenderer.WriteInfo($"  Scripts:       {info.Scripts.Count}");
                ConsoleRenderer.WriteInfo($"  References:    {info.References.Count}");
                ConsoleRenderer.WriteInfo($"  Directory:     {info.DirectoryPath}");
                break;

            case "create":
                await CreateSkillAsync();
                break;

            case "edit" when args.Length >= 2:
                await EditSkillAsync(args[1]);
                break;

            case "remove" or "delete" when args.Length >= 2:
                await RemoveSkillAsync(args[1]);
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
    }

    private async Task CreateSkillAsync()
    {
        string projectDir = SkillLoader.ResolveSkillsDirectory(_settings.SkillsDirectory);
        string globalDir = SkillLoader.ResolveGlobalSkillsDirectory();

        AskQuestionsInteractionRequest request = new()
        {
            Title = "Create a new skill",
            Message = "Answer a few questions; the model will draft the SKILL.md from your answers.",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "name", Prompt = "Skill name", Required = true,
                    Description = "Lowercase letters, digits, and hyphens. Becomes the folder name.",
                },
                new InteractiveQuestionDefinition
                {
                    Key = "description", Prompt = "One-line description", Required = true,
                    Description = "What it does and when the agent should use it (used for triggering).",
                },
                WizardSupport.SaveLocationQuestion(projectDir, globalDir),
                WizardSupport.ToolSelectQuestion("tools", "Allowed tools (optional)",
                    "Select tools to pre-approve for this skill. Pick numbers, add a custom id, or leave blank for none.",
                    _builder.Agent.ToolList.Keys),
                new InteractiveQuestionDefinition
                {
                    Key = "brief", Prompt = "Describe the skill's purpose and steps", Required = true,
                    Description = "A short brief the model will expand into the full instructions.",
                },
            ],
        };

        AskQuestionsInteractionResponse answers = await _interaction.AskQuestionsAsync(request);

        string name = answers.Text("name");
        string description = answers.Text("description");
        string brief = answers.Text("brief");
        List<string> tools = answers.Selected("tools");
        string location = answers.Choice("location", WizardSupport.LocationProject);

        string slug = SkillLoader.Slugify(name);
        if (!SkillLoader.IsValidSkillName(slug))
        {
            ConsoleRenderer.WriteError($"'{name}' can't be turned into a valid skill name (lowercase letters, digits, hyphens).");
            return;
        }
        if (_skillManager.GetSkill(slug) is not null)
        {
            ConsoleRenderer.WriteError($"A skill named '{slug}' already exists. Use /skill edit {slug} instead.");
            return;
        }

        string rootDir = location == WizardSupport.LocationGlobal ? globalDir : projectDir;

        ConsoleRenderer.WriteInfo("Drafting SKILL.md with the authoring assistant...");
        string context =
            $"Skill name: {slug}\nDescription: {description}\n" +
            $"Allowed tools: {(tools.Count > 0 ? string.Join(' ', tools) : "none")}\nBrief: {brief}";
        string body = await AuthoringAssistant.DraftAsync(
            _providers.Api, _providers.ActiveModel, AuthoringAssistant.SkillAuthorPrompt, context);

        string skillMdPath;
        try
        {
            skillMdPath = _skillManager.CreateSkill(rootDir, slug, description, body, allowedTools: tools, fullSkeleton: true);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to write skill: {ex.Message}");
            return;
        }

        ReloadSkills(projectDir, globalDir);
        ConsoleRenderer.WriteSuccess($"Created skill '{slug}' ({location}).");
        PreviewSkill(slug, skillMdPath, body);
    }

    private async Task EditSkillAsync(string name)
    {
        Skill? skill = _skillManager.GetSkill(name);
        if (skill is null)
        {
            ConsoleRenderer.WriteError($"Skill '{name}' not found. Use /skill list to see available skills.");
            return;
        }

        SkillLoader.LoadInstructions(skill);
        string currentBody = skill.Instructions ?? string.Empty;

        AskQuestionsInteractionRequest request = new()
        {
            Title = $"Edit skill '{skill.Name}'",
            Message = "Leave a field blank to keep its current value.",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "description", Prompt = "Description", Required = false,
                    Description = $"Current: {skill.Description}",
                },
                WizardSupport.ToolSelectQuestion("tools", "Allowed tools",
                    $"Current: {(skill.AllowedTools.Count > 0 ? string.Join(' ', skill.AllowedTools) : "none")}. Select to replace, or leave blank to keep.",
                    _builder.Agent.ToolList.Keys),
                new InteractiveQuestionDefinition
                {
                    Key = "brief", Prompt = "Re-draft instructions from a new brief? (optional)", Required = false,
                    Description = "Describe changes to regenerate the body. Leave blank to keep the current instructions.",
                },
            ],
        };

        AskQuestionsInteractionResponse answers = await _interaction.AskQuestionsAsync(request);

        string description = answers.Text("description", skill.Description);
        List<string> selectedTools = answers.Selected("tools");
        List<string> tools = selectedTools.Count > 0 ? selectedTools : skill.AllowedTools;
        string brief = answers.Text("brief");

        string body = currentBody;
        if (!string.IsNullOrWhiteSpace(brief))
        {
            ConsoleRenderer.WriteInfo("Re-drafting SKILL.md with the authoring assistant...");
            string context =
                $"Skill name: {skill.Name}\nDescription: {description}\n" +
                $"Allowed tools: {(tools.Count > 0 ? string.Join(' ', tools) : "none")}\n" +
                $"Existing instructions:\n{currentBody}\n\nRequested changes: {brief}";
            body = await AuthoringAssistant.DraftAsync(
                _providers.Api, _providers.ActiveModel, AuthoringAssistant.SkillAuthorPrompt, context);
        }

        string? skillMdPath = _skillManager.UpdateSkill(skill.Name, description, body,
            license: skill.License, compatibility: skill.Compatibility, allowedTools: tools);
        if (skillMdPath is null)
        {
            ConsoleRenderer.WriteError($"Failed to update skill '{name}'.");
            return;
        }

        string projectDir = SkillLoader.ResolveSkillsDirectory(_settings.SkillsDirectory);
        string globalDir = SkillLoader.ResolveGlobalSkillsDirectory();
        ReloadSkills(projectDir, globalDir);

        ConsoleRenderer.WriteSuccess($"Updated skill '{skill.Name}'.");
        PreviewSkill(skill.Name, skillMdPath, body);
    }

    private async Task RemoveSkillAsync(string name)
    {
        Skill? skill = _skillManager.GetSkill(name);
        if (skill is null)
        {
            ConsoleRenderer.WriteError($"Skill '{name}' not found. Use /skill list to see available skills.");
            return;
        }

        bool confirmed = await _interaction.ConfirmAsync(
            $"Remove skill '{skill.Name}'",
            $"Delete '{skill.Name}' and its entire folder ({skill.DirectoryPath})? This cannot be undone.");
        if (!confirmed)
        {
            ConsoleRenderer.WriteInfo("Cancelled. Nothing was deleted.");
            return;
        }

        if (!_skillManager.DeleteSkill(skill.Name))
        {
            ConsoleRenderer.WriteError($"Failed to remove skill '{name}'.");
            return;
        }

        string projectDir = SkillLoader.ResolveSkillsDirectory(_settings.SkillsDirectory);
        string globalDir = SkillLoader.ResolveGlobalSkillsDirectory();
        ReloadSkills(projectDir, globalDir);
        ConsoleRenderer.WriteSuccess($"Removed skill '{skill.Name}'.");
    }

    private void ReloadSkills(string projectDir, string globalDir)
    {
        _skillManager.LoadSkills(projectDir, globalDir);
        _builder.RebuildForSkillChange(_runtimeEventHandler);
    }

    private static void PreviewSkill(string name, string skillMdPath, string body)
    {
        ConsoleRenderer.WriteInfo($"  Saved to: {skillMdPath}");
        ConsoleRenderer.WriteInfo("  Preview:");
        string[] lines = body.Split('\n');
        int preview = Math.Min(12, lines.Length);
        for (int i = 0; i < preview; i++)
            ConsoleRenderer.WriteInfo($"    {lines[i].TrimEnd()}");
        if (lines.Length > preview)
            ConsoleRenderer.WriteInfo($"    ... ({lines.Length - preview} more lines)");
        ConsoleRenderer.WriteInfo($"  Refine it with /skill edit {name}, or open the file above in your editor.");
    }
}
