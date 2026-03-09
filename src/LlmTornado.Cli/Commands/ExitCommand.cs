using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;

namespace LlmTornado.Cli.Commands;

internal sealed class ExitCommand : ICliCommand
{
    public string Name => "exit";
    public string Description => "Exit the CLI (auto-saves current conversation)";
    public string Usage => "/exit";

    private readonly ConversationMemoryManager _memoryManager;
    private readonly SqliteConversationStore _store;
    private readonly CliAgentBuilder _builder;

    public ExitCommand(ConversationMemoryManager memoryManager, SqliteConversationStore store, CliAgentBuilder builder)
    {
        _memoryManager = memoryManager;
        _store = store;
        _builder = builder;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (_memoryManager.Messages.Count > 0)
        {
            _store.Save(
                [.. _memoryManager.Messages],
                _builder.ActiveModel.Name,
                null);
            ConsoleRenderer.WriteInfo("Conversation auto-saved.");
        }
        ConsoleRenderer.WriteInfo("Goodbye.");
        return Task.FromResult(false); // false = exit REPL
    }
}
