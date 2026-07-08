using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Runtime configuration that makes <see cref="ConversationMemoryManager"/> the single coordinator for the
/// canonical conversation. The runtime <see cref="SingletonRuntimeConfiguration.Conversation"/> remains the
/// only message set fed to the model; after each turn this config syncs the complete set (including tool
/// messages) into memory, runs compression / budget enforcement, and pushes the result back so the next
/// request reflects the compressed state. This is what makes compression actually shrink the payload and
/// keeps persistence and request-assembly working from a single source of truth.
/// </summary>
public sealed class ManagedConversationRuntimeConfiguration : SingletonRuntimeConfiguration
{
    private readonly ConversationMemoryManager _memory;
    private readonly Func<ChatMessage>? _environmentMessageFactory;

    /// <summary>
    /// True if the most recent turn triggered compression and/or a budget trim. The host can surface a notice.
    /// </summary>
    public bool LastTurnCompressed { get; private set; }

    public ManagedConversationRuntimeConfiguration(
        TornadoAgent agent,
        ConversationMemoryManager memory,
        Func<ChatMessage>? environmentMessageFactory = null)
        : base(agent)
    {
        _memory = memory;
        _environmentMessageFactory = environmentMessageFactory;

        // Persistence live from the first turn (fixes the "no conversation id at startup" gap).
        _memory.EnsureActiveConversation();

        // Preserve history across rebuilds (model/agent/skill switch re-creates this config).
        RehydrateConversation();
    }

    public override async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        ChatMessage result = await base.AddToChatAsync(message, cancellationToken);

        // Prefix this assistant result after generation but before memory sync so future turns see when it happened.
        DateTimeOffset completedAt = DateTimeOffset.Now;
        MessageTimestampPrefixer.Prefix(result, "assistant", completedAt);
        ChatMessage? storedResult = Conversation.Messages.LastOrDefault(msg => msg.Id == result.Id)
                                  ?? Conversation.Messages.LastOrDefault(msg => msg.Role == ChatMessageRoles.Assistant);
        if (storedResult is not null && !ReferenceEquals(storedResult, result))
            MessageTimestampPrefixer.Prefix(storedResult, "assistant", completedAt);

        // Capture the complete runtime set (user + tool calls/results + assistant) into memory and persist it.
        _memory.SyncFrom(Conversation.Messages);

        // Compress + enforce the hard budget; if the set changed, push it back so the next request shrinks.
        LastTurnCompressed = await _memory.MaybeSummarize(cancellationToken);
        if (LastTurnCompressed)
            RehydrateConversation();

        return result;
    }

    /// <summary>
    /// Load a stored conversation and make it the live runtime + memory state (binds the id for incremental
    /// persistence and feeds the loaded history to the model).
    /// </summary>
    public void LoadConversation(string conversationId)
    {
        _memory.LoadConversation(conversationId);
        RehydrateConversation();
    }

    /// <summary>
    /// Start a fresh conversation, clearing both memory and the runtime.
    /// </summary>
    public void NewConversation()
    {
        _memory.NewConversation();
        RehydrateConversation();
    }

    /// <summary>
    /// Replace the runtime conversation with the current (compressed) memory set, pinning a fresh
    /// environment message first. Stale env messages from persisted history are dropped so a
    /// resumed or rebuilt session never carries two (or an outdated) environment block.
    /// </summary>
    private void RehydrateConversation()
    {
        Conversation.Clear();

        if (_environmentMessageFactory is not null)
            Conversation.AppendMessage(_environmentMessageFactory());

        foreach (ChatMessage msg in _memory.GetMessagesForAgent())
        {
            if (_environmentMessageFactory is not null && IsEnvironmentMessage(msg))
                continue;
            Conversation.AppendMessage(msg);
        }
    }

    private static bool IsEnvironmentMessage(ChatMessage msg) =>
        msg.Role == ChatMessageRoles.User
        && msg.Content is not null
        && msg.Content.StartsWith(AgentBuilder.EnvironmentTag, StringComparison.Ordinal);
}
