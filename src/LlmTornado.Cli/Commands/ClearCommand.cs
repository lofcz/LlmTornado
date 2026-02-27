namespace LlmTornado.Cli.Commands;

internal sealed class ClearCommand : ICliCommand
{
    public string Name => "clear";
    public string Description => "Clear the console screen";
    public string Usage => "/clear";

    public Task<bool> ExecuteAsync(string[] args)
    {
        Console.Clear();
        return Task.FromResult(true);
    }
}
