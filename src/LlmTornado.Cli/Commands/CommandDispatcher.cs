namespace LlmTornado.Cli.Commands;

/// <summary>
/// Interface for a CLI slash command.
/// </summary>
internal interface ICliCommand
{
    string Name { get; }
    string Description { get; }
    string Usage { get; }
    Task<bool> ExecuteAsync(string[] args);
}

internal interface IRawCliCommand : ICliCommand
{
    Task<bool> ExecuteRawAsync(string rawArgs);
}

/// <summary>
/// Dispatches /commands to their handlers.
/// </summary>
internal sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICliCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICliCommand command) => _commands[command.Name] = command;

    public bool IsCommand(string input) => input.TrimStart().StartsWith('/');

    public async Task<bool> DispatchAsync(string input)
    {
        string trimmed = input.TrimStart().TrimStart('/');
        string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return true;

        string commandName = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : [];

        if (_commands.TryGetValue(commandName, out ICliCommand? command))
        {
            if (command is IRawCliCommand rawCommand)
            {
                int commandStart = trimmed.IndexOf(commandName, StringComparison.OrdinalIgnoreCase);
                int argsStart = commandStart + commandName.Length;
                string rawArgs = argsStart < trimmed.Length ? trimmed[argsStart..].TrimStart() : string.Empty;
                return await rawCommand.ExecuteRawAsync(rawArgs);
            }

            return await command.ExecuteAsync(args);
        }

        ConsoleRenderer.WriteError($"Unknown command: /{commandName}. Type /help for available commands.");
        return true;
    }

    public IReadOnlyDictionary<string, ICliCommand> Commands => _commands;
}
