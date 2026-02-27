using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Skills;

namespace LlmTornado.Cli.Commands;

internal sealed class SkillCommand : ICliCommand
{
    public string Name => "skill";
    public string Description => "Manage skills (list, enable, disable, info)";
    public string Usage => "/skill [list | enable <name> | disable <name> | info <name>]";

    private readonly CliSkillManager _skillManager;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public SkillCommand(CliSkillManager skillManager, CliAgentBuilder builder, Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _skillManager = skillManager;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            List<CliSkill> enabled = _skillManager.GetEnabledSkills();
            List<CliSkill> all = _skillManager.GetAllSkills();
            ConsoleRenderer.WriteInfo($"{enabled.Count}/{all.Count} skills enabled.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                List<CliSkill> skills = _skillManager.GetAllSkills();
                if (skills.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No skills found. Place skills in the ./skills/ directory.");
                    break;
                }
                foreach (CliSkill skill in skills)
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
                CliSkill? info = _skillManager.GetSkill(args[1]);
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

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
