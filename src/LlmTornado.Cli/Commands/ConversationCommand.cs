using LlmTornado.Cli.Core.Memory;

namespace LlmTornado.Cli.Commands;

internal sealed class ConversationCommand : ICliCommand
{
    public string Name => "conversation";
    public string Description => "Save, load, list, delete conversations";
    public string Usage => "/conversation [save [label] | load <id> | list | delete <id> | new]";

    private readonly ConversationMemoryManager _memoryManager;
    private readonly ConversationStore _store;
    private readonly CliAgentBuilder _builder;

    public ConversationCommand(ConversationMemoryManager memoryManager, ConversationStore store, CliAgentBuilder builder)
    {
        _memoryManager = memoryManager;
        _store = store;
        _builder = builder;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Current conversation: {_memoryManager.Messages.Count} messages.");
            ConsoleRenderer.WriteInfo($"Use /conversation save, load, list, delete, or new.");
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "save":
                string? label = args.Length >= 2 ? string.Join(" ", args[1..]) : null;
                string activeSkill = string.Join(",", new List<string>());
                string id = _store.Save(
                    [.. _memoryManager.Messages],
                    _builder.ActiveModel.Name,
                    null,
                    label);
                ConsoleRenderer.WriteSuccess($"Saved as: {id}");
                break;

            case "load" when args.Length >= 2:
                string loadId = args[1];
                List<Chat.ChatMessage>? messages = _store.Load(loadId);
                if (messages is null)
                {
                    ConsoleRenderer.WriteError($"Conversation '{loadId}' not found.");
                    break;
                }
                _memoryManager.LoadConversation(messages);
                ConsoleRenderer.WriteSuccess($"Loaded conversation: {loadId} ({messages.Count} messages)");
                break;

            case "list":
                List<ConversationMetadata> conversations = _store.List();
                if (conversations.Count == 0)
                {
                    ConsoleRenderer.WriteInfo("No saved conversations.");
                    break;
                }
                foreach (ConversationMetadata meta in conversations)
                {
                    string display = meta.Label ?? meta.Id;
                    string preview = meta.FirstMessagePreview is not null ? $" — {meta.FirstMessagePreview}" : "";
                    ConsoleRenderer.WriteInfo($"  {meta.Id}  [{meta.MessageCount} msgs] {display}{preview}");
                }
                break;

            case "delete" when args.Length >= 2:
                if (_store.Delete(args[1]))
                    ConsoleRenderer.WriteSuccess($"Deleted: {args[1]}");
                else
                    ConsoleRenderer.WriteError($"Conversation '{args[1]}' not found.");
                break;

            case "new":
                _memoryManager.NewConversation();
                ConsoleRenderer.WriteSuccess("Started new conversation.");
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
