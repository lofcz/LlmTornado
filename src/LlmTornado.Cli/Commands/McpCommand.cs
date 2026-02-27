using LlmTornado.Cli.Mcp;

namespace LlmTornado.Cli.Commands;

internal sealed class McpCommand : ICliCommand
{
    public string Name => "mcp";
    public string Description => "View and reload MCP server connections";
    public string Usage => "/mcp [status | reload]";

    private readonly McpConfigLoader _mcpLoader;
    private readonly CliAgentBuilder _builder;

    public McpCommand(McpConfigLoader mcpLoader, CliAgentBuilder builder)
    {
        _mcpLoader = mcpLoader;
        _builder = builder;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            if (_mcpLoader.ServerStatuses.Count == 0)
            {
                ConsoleRenderer.WriteInfo("No MCP servers configured.");
                return true;
            }
            foreach (McpServerStatus status in _mcpLoader.ServerStatuses)
            {
                if (status.Connected)
                    ConsoleRenderer.WriteInfo($"  ✓ {status.Name} ({status.Type}) — {status.ToolCount} tools");
                else
                    ConsoleRenderer.WriteError($"  ✗ {status.Name} ({status.Type}) — {status.Error}");
            }
            return true;
        }

        if (args[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleRenderer.WriteInfo("Reloading MCP servers...");
            await _mcpLoader.ReloadAsync(ConsoleRenderer.WriteInfo);
            ConsoleRenderer.WriteSuccess("MCP reload complete.");
            return true;
        }

        ConsoleRenderer.WriteError($"Usage: {Usage}");
        return true;
    }
}
