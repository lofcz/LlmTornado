using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Cli.Input;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// /resume — pick a recent conversation and continue it. Complements the --continue/--resume
/// startup flags with an in-session picker.
/// </summary>
internal sealed class ResumeCommand : ICliCommand
{
    private const int PickerSize = 10;

    public string Name => "resume";
    public string Description => "Resume a recent conversation (picker or by id)";
    public string Usage => "/resume [<id>]";

    private readonly SqliteConversationStore _store;
    private readonly ConversationMemoryManager _memoryManager;
    private readonly CliAgentBuilder _builder;

    public ResumeCommand(
        SqliteConversationStore store,
        ConversationMemoryManager memoryManager,
        CliAgentBuilder builder)
    {
        _store = store;
        _memoryManager = memoryManager;
        _builder = builder;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            Load(args[0]);
            return Task.FromResult(true);
        }

        List<ConversationMetadata> recent = _store.List().Take(PickerSize).ToList();
        if (recent.Count == 0)
        {
            ConsoleRenderer.WriteInfo("No saved conversations to resume.");
            return Task.FromResult(true);
        }

        for (int i = 0; i < recent.Count; i++)
        {
            ConversationMetadata meta = recent[i];
            string current = meta.Id == _memoryManager.ConversationId ? " (current)" : "";
            string display = meta.Label ?? meta.FirstMessagePreview ?? "(no preview)";
            ConsoleRenderer.WriteInfo(
                $"  {i + 1}. {meta.Id}  [{meta.MessageCount} msgs, {meta.UpdatedAt:yyyy-MM-dd HH:mm}] {display}{current}");
        }

        using IDisposable inputScope = ConsoleInputGate.Suspend();
        Console.Write("Resume which conversation? (number or Enter to cancel): ");
        string? choice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(choice))
        {
            ConsoleRenderer.WriteInfo("Cancelled.");
            return Task.FromResult(true);
        }

        if (int.TryParse(choice, out int index) && index >= 1 && index <= recent.Count)
            Load(recent[index - 1].Id);
        else
            ConsoleRenderer.WriteError($"Invalid selection '{choice}'.");

        return Task.FromResult(true);
    }

    private void Load(string id)
    {
        if (_store.Load(id) is null)
        {
            ConsoleRenderer.WriteError($"Conversation '{id}' not found.");
            return;
        }

        // Delegate to the managed config so the id is bound and history reaches the runtime.
        if (_builder.ConversationConfig is not null)
            _builder.ConversationConfig.LoadConversation(id);
        else
            _memoryManager.LoadConversation(id);

        ConsoleRenderer.WriteSuccess($"Resumed conversation: {id} ({_memoryManager.Messages.Count} messages)");
    }
}
