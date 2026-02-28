using System.Diagnostics;
using LlmTornado.Agents.DataModels;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Mcp;

namespace LlmTornado.Cli.Commands;

internal sealed class McpCommand : ICliCommand
{
    public string Name => "mcp";
    public string Description => "View, reload, and edit MCP server configuration";
    public string Usage => "/mcp [status | reload | edit]";

    private readonly McpConfigLoader _mcpLoader;
    private readonly CliAgentBuilder _builder;
    private readonly AgentSettings _settings;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public McpCommand(McpConfigLoader mcpLoader, CliAgentBuilder builder, AgentSettings settings, Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _mcpLoader = mcpLoader;
        _builder = builder;
        _settings = settings;
        _runtimeEventHandler = runtimeEventHandler;
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
            _builder.RebuildForSkillChange(_runtimeEventHandler);
            ConsoleRenderer.WriteSuccess($"MCP reload complete. {_mcpLoader.AllTools.Count} tool(s) available.");
            return true;
        }

        if (args[0].Equals("edit", StringComparison.OrdinalIgnoreCase))
        {
            OpenConfigInEditor();
            return true;
        }

        ConsoleRenderer.WriteError($"Usage: {Usage}");
        return true;
    }

    private void OpenConfigInEditor()
    {
        string configPath = McpConfigLoader.ResolveDefaultMcpConfigPath(_settings.McpConfigPath);
        bool created = false;

        if (!File.Exists(configPath))
        {
            try
            {
                string? dir = Path.GetDirectoryName(configPath);
                if (dir is not null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string template = """
                    {
                      "servers": [
                        {
                          "type": "stdio",
                          "name": "example-server",
                          "command": "npx",
                          "args": ["-y", "@example/mcp-server"]
                        }
                      ]
                    }
                    """;
                File.WriteAllText(configPath, template);
                created = true;
            }
            catch (Exception ex)
            {
                ConsoleRenderer.WriteError($"Failed to create {configPath}: {ex.Message}");
                return;
            }
        }

        if (created)
            ConsoleRenderer.WriteInfo($"Created new config: {configPath}");

        ConsoleRenderer.WriteInfo($"Opening {configPath}...");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to open editor: {ex.Message}");
            ConsoleRenderer.WriteInfo($"File location: {configPath}");
        }
    }
}
