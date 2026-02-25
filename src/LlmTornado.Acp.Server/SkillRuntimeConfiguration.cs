using LlmTornado.Acp.Server.Skills;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Acp.Server;

/// <summary>
/// Custom <see cref="IRuntimeConfiguration"/> that configures an agent from an <see cref="AgentSkill"/>.
/// Provides skill-aware agent setup with optimized instructions and tool selection.
/// </summary>
internal sealed class SkillRuntimeConfiguration : IRuntimeConfiguration
{
    public ChatRuntime Runtime { get; set; }
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }
    public Func<string, ValueTask<bool>>? OnRuntimeRequestEvent { get; set; }
    public CancellationTokenSource Cts { get; set; } = new();

    /// <summary>
    /// The agent skill powering this configuration.
    /// </summary>
    public AgentSkill Skill { get; }

    /// <summary>
    /// The underlying agent.
    /// </summary>
    public TornadoAgent Agent { get; }

    /// <summary>
    /// Current conversation state.
    /// </summary>
    public Conversation Conversation { get; set; }

    /// <summary>
    /// Creates a skill-powered runtime configuration.
    /// </summary>
    /// <param name="api">The Tornado API client.</param>
    /// <param name="model">The chat model to use.</param>
    /// <param name="skill">The agent skill definition.</param>
    /// <param name="cwd">The user's working directory.</param>
    /// <param name="tools">Optional filesystem tools.</param>
    public SkillRuntimeConfiguration(
        TornadoApi api,
        ChatModel model,
        AgentSkill skill,
        string cwd,
        List<Tool>? tools = null)
    {
        Skill = skill;

        string instructions = BuildInstructions(skill, cwd);
        List<Delegate>? delegates = skill.UseTools && tools is not null
            ? tools.ConvertAll<Delegate>(t => t.Delegate!)
            : null;

        Agent = new TornadoAgent(
            client: api,
            model: model,
            name: $"ACP-{skill.Name}",
            instructions: instructions,
            tools: delegates,
            streaming: true);

        Conversation = Agent.Client.Chat.CreateConversation(Agent.Options);
    }

    public void OnRuntimeInitialized()
    {
    }

    public void CancelRuntime()
    {
        Cts.Cancel();
        OnRuntimeEvent?.Invoke(new ChatRuntimeCancelledEvent(Runtime.Id));
    }

    public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        OnRuntimeEvent?.Invoke(new ChatRuntimeStartedEvent(Runtime.Id));

        Conversation.AppendMessage(message);

        Conversation = await Agent.Run(
            appendMessages: Conversation.Messages.ToList(),
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id));
                return ValueTask.CompletedTask;
            },
            toolPermissionHandle: OnRuntimeRequestEvent,
            cancellationToken: cancellationToken);

        OnRuntimeEvent?.Invoke(new ChatRuntimeCompletedEvent(Runtime.Id));
        return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
    }

    public ChatMessage GetLastMessage()
    {
        return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
    }

    public List<ChatMessage> GetMessages()
    {
        return Conversation?.Messages.ToList() ?? [];
    }

    public void ClearMessages()
    {
        Conversation?.Clear();
    }

    /// <summary>
    /// Builds the full system prompt from skill instructions and runtime context.
    /// </summary>
    private static string BuildInstructions(AgentSkill skill, string cwd)
    {
        string acpRoot = TornadoAcpRuntime.ResolveAcpRootPath(cwd);
        string contextSuffix = $"\n\nThe user's current working directory is: {cwd}\nTool access is restricted to: {acpRoot}";

        // For non-orchestrated skills, use the full instructions body (excluding stage sections)
        string instructions = skill.Instructions;

        // Strip out stage: sections if present (they're only used by orchestrated pipelines)
        if (skill.StageInstructions.Count > 0)
        {
            int stageStart = instructions.IndexOf("## stage:", StringComparison.OrdinalIgnoreCase);

            if (stageStart > 0)
            {
                instructions = instructions.Substring(0, stageStart).Trim();
            }
        }

        return $"{instructions}{contextSuffix}";
    }
}
