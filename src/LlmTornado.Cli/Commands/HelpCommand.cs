namespace LlmTornado.Cli.Commands;

internal sealed class HelpCommand : ICliCommand
{
    public string Name => "help";
    public string Description => "Show available commands";
    public string Usage => "/help [command]";

    private readonly CommandDispatcher _dispatcher;

    public HelpCommand(CommandDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length > 0 && _dispatcher.Commands.TryGetValue(args[0], out ICliCommand? cmd))
        {
            ConsoleRenderer.WriteInfo($"/{cmd.Name} — {cmd.Description}");
            ConsoleRenderer.WriteInfo($"Usage: {cmd.Usage}");
        }
        else
        {
            ConsoleRenderer.WriteInfo("Available commands:");
            foreach ((string name, ICliCommand command) in _dispatcher.Commands.OrderBy(c => c.Key))
            {
                ConsoleRenderer.WriteInfo($"  /{name,-20} {command.Description}");
            }
        }
        return Task.FromResult(true);
    }
}
