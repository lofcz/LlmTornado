namespace LlmTornado.Cli.Core.Mcp;

public sealed record BuiltInMcpToolDefinition(string Name, string Description, bool IsFilesystemTool = false, bool IsTerminalTool = false);

public sealed record BuiltInMcpServerDefinition(string Name, string Description, McpServerEntry Entry, IReadOnlyList<BuiltInMcpToolDefinition> Tools);

/// <summary>
/// Built-in MCP servers layered on top of user-provided mcp.json configuration.
/// </summary>
public static class BuiltInMcpServerCatalog
{
    public const string DesktopCommanderServerName = "desktop-commander";

    private static readonly BuiltInMcpToolDefinition[] DesktopCommanderTools =
    [
        new("read_file", "Read file contents from the local filesystem.", IsFilesystemTool: true),
        new("read_multiple_files", "Read multiple files in one call.", IsFilesystemTool: true),
        new("write_file", "Write or append file contents.", IsFilesystemTool: true),
        new("write_pdf", "Create or update PDF files.", IsFilesystemTool: true),
        new("create_directory", "Create a directory.", IsFilesystemTool: true),
        new("list_directory", "List files and directories.", IsFilesystemTool: true),
        new("move_file", "Move or rename files and directories.", IsFilesystemTool: true),
        new("start_search", "Start a filesystem search.", IsFilesystemTool: true),
        new("get_more_search_results", "Read more search results.", IsFilesystemTool: true),
        new("stop_search", "Stop an active search.", IsFilesystemTool: true),
        new("list_searches", "List active searches.", IsFilesystemTool: true),
        new("get_file_info", "Read metadata for a file or directory.", IsFilesystemTool: true),
        new("edit_block", "Apply targeted text replacements.", IsFilesystemTool: true),
        new("start_process", "Start a terminal process.", IsTerminalTool: true),
        new("interact_with_process", "Send input to a running terminal process.", IsTerminalTool: true),
        new("read_process_output", "Read output from a running terminal process.", IsTerminalTool: true),
        new("force_terminate", "Force terminate a running terminal process.", IsTerminalTool: true),
        new("list_sessions", "List active terminal sessions.", IsTerminalTool: true),
        new("list_processes", "List running processes.", IsTerminalTool: true),
        new("kill_process", "Terminate a process by PID.", IsTerminalTool: true)
    ];

    public static IReadOnlyList<BuiltInMcpServerDefinition> GetDefinitions(string? workingDirectory)
    {
        string cwd = string.IsNullOrWhiteSpace(workingDirectory)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(workingDirectory);

        McpServerEntry desktopCommander = new()
        {
            Name = DesktopCommanderServerName,
            Type = "stdio",
            Command = "npx",
            Args = ["-y", "@wonderwhy-er/desktop-commander@latest", "--no-onboarding"],
            Cwd = cwd,
            AllowedTools = [.. DesktopCommanderTools.Select(x => x.Name)],
            Source = McpServerSource.BuiltIn
        };

        return
        [
            new BuiltInMcpServerDefinition(
                DesktopCommanderServerName,
                "Built-in Desktop Commander server for filesystem and terminal tools.",
                desktopCommander,
                DesktopCommanderTools)
        ];
    }

    public static BuiltInMcpServerDefinition? GetDefinition(string serverName, string? workingDirectory)
        => GetDefinitions(workingDirectory).FirstOrDefault(x => x.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
}