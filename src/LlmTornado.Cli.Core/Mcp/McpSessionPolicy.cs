using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core.Mcp;

/// <summary>
/// Effective MCP and local-tool sandbox policy for the current session.
/// </summary>
public sealed class McpSessionPolicy
{
    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; init; } = string.Empty;

    [JsonPropertyName("filesystem_whitelist")]
    public HashSet<string> FilesystemWhitelist { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("terminal_directory_whitelist")]
    public HashSet<string> TerminalDirectoryWhitelist { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("allowed_commands")]
    public HashSet<string> AllowedCommands { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("blocked_commands")]
    public HashSet<string> BlockedCommands { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> GetAllowedFilesystemDirectories()
        => GetAllowedDirectories(WorkingDirectory, FilesystemWhitelist);

    public IReadOnlyList<string> GetAllowedTerminalDirectories()
        => GetAllowedDirectories(WorkingDirectory, TerminalDirectoryWhitelist);

    public bool IsFilesystemPathAllowed(string? path)
        => IsPathAllowed(path, GetAllowedFilesystemDirectories());

    public bool IsTerminalDirectoryAllowed(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        return IsPathAllowed(path, GetAllowedTerminalDirectories());
    }

    public bool IsCommandAllowed(string? commandLine)
    {
        string? commandName = NormalizeCommandName(commandLine);
        if (string.IsNullOrWhiteSpace(commandName))
            return true;

        if (BlockedCommands.Contains(commandName))
            return false;

        return AllowedCommands.Count == 0 || AllowedCommands.Contains(commandName);
    }

    public static string NormalizeCommandName(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return string.Empty;

        string trimmed = commandLine.Trim();
        if (trimmed.StartsWith('"'))
        {
            int endQuote = trimmed.IndexOf('"', 1);
            trimmed = endQuote > 0 ? trimmed[1..endQuote] : trimmed.Trim('"');
        }
        else
        {
            int firstSpace = trimmed.IndexOf(' ');
            if (firstSpace > 0)
                trimmed = trimmed[..firstSpace];
        }

        return Path.GetFileName(trimmed).Trim().ToLowerInvariant();
    }

    public static McpSessionPolicy FromSettings(AgentSettings settings, string? workingDirectory)
    {
        string cwd = NormalizePath(workingDirectory ?? Environment.CurrentDirectory, null);

        return new McpSessionPolicy
        {
            WorkingDirectory = cwd,
            FilesystemWhitelist = BuildPathSet(settings.FilesystemWhitelist, cwd),
            TerminalDirectoryWhitelist = BuildPathSet(settings.TerminalDirectoryWhitelist, cwd),
            AllowedCommands = BuildCommandSet(settings.AllowedCommands),
            BlockedCommands = BuildCommandSet(settings.BlockedCommands)
        };
    }

    internal static string NormalizePath(string path, string? baseDirectory)
    {
        string candidate = path;
        if (!Path.IsPathRooted(candidate) && !string.IsNullOrWhiteSpace(baseDirectory))
            candidate = Path.Combine(baseDirectory, candidate);

        return Path.GetFullPath(candidate);
    }

    private static bool IsPathAllowed(string? path, IReadOnlyList<string> allowedRoots)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string resolved = NormalizePath(path, null);
        return allowedRoots.Any(root => IsWithinDirectory(resolved, root));
    }

    private static IReadOnlyList<string> GetAllowedDirectories(string workingDirectory, IEnumerable<string> extraRoots)
    {
        HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePath(workingDirectory, null)
        };

        foreach (string root in extraRoots)
        {
            if (!string.IsNullOrWhiteSpace(root))
                allowed.Add(NormalizePath(root, workingDirectory));
        }

        return [.. allowed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)];
    }

    private static HashSet<string> BuildPathSet(IEnumerable<string>? paths, string workingDirectory)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        if (paths is null)
            return result;

        foreach (string path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path))
                result.Add(NormalizePath(path, workingDirectory));
        }

        return result;
    }

    private static HashSet<string> BuildCommandSet(IEnumerable<string>? commands)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        if (commands is null)
            return result;

        foreach (string command in commands)
        {
            string normalized = NormalizeCommandName(command);
            if (!string.IsNullOrWhiteSpace(normalized))
                result.Add(normalized);
        }

        return result;
    }

    private static bool IsWithinDirectory(string path, string root)
    {
        string normalizedPath = Path.TrimEndingDirectorySeparator(path);
        string normalizedRoot = Path.TrimEndingDirectorySeparator(root);

        if (normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return true;

        string prefix = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
               || normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}