using System.Text;
using System.Text.RegularExpressions;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core.Tools.Native;

/// <summary>
/// Dependencies for the native tools. Working directory and policy are resolved per call so
/// /cd and settings changes take effect without a rebuild.
/// </summary>
public sealed class NativeToolContext
{
    public required Func<string> GetWorkingDirectory { get; init; }
    public required AgentSettings Settings { get; init; }

    public McpSessionPolicy Policy => McpSessionPolicy.FromSettings(Settings, GetWorkingDirectory());

    public string Resolve(string? path)
    {
        string cwd = GetWorkingDirectory();
        if (string.IsNullOrWhiteSpace(path))
            return cwd;
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(cwd, path));
    }
}

/// <summary>
/// C#-native file/search/shell tools so the agent works offline without the Node-based
/// Desktop Commander MCP server. All paths go through <see cref="McpSessionPolicy"/> —
/// the same sandbox applied to MCP tools. Schemas are intentionally flat and small so
/// weak local models can call them reliably.
/// </summary>
public static class NativeToolkit
{
    private const int MaxReadLines = 2000;
    private const int MaxLineChars = 500;
    private const int MaxOutputChars = 30_000;
    private const int MaxWalkFiles = 20_000;
    private const int DefaultGrepResults = 100;
    private const int DefaultShellTimeoutSeconds = 120;
    private const int MaxShellTimeoutSeconds = 600;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "node_modules", "bin", "obj", ".vs", ".idea", "__pycache__",
    };

    /// <summary>Tools that never mutate anything; eligible for auto-approval.</summary>
    public static readonly IReadOnlyList<string> ReadOnlyToolNames =
        ["read_file", "glob", "grep", "list_dir"];

    public static List<Tool> Build(NativeToolContext ctx)
    {
        return
        [
            new Tool(new Func<ReadFileRequest, string>(req => ReadFile(ctx, req)), "read_file",
                "Read a text file. Returns numbered lines. Optional offset (1-based start line) and limit (line count)."),

            new Tool(new Func<WriteFileRequest, string>(req => WriteFile(ctx, req)), "write_file",
                "Write content to a file, creating directories as needed. Overwrites existing content."),

            new Tool(new Func<EditFileRequest, string>(req => EditFile(ctx, req)), "edit_file",
                "Replace text in a file. old_string must match exactly and be unique unless replace_all is true."),

            new Tool(new Func<GlobRequest, string>(req => GlobFiles(ctx, req)), "glob",
                "Find files by glob pattern (e.g. \"**/*.cs\", \"src/*.json\"). Optional path = directory to search (default: working directory)."),

            new Tool(new Func<GrepRequest, string>(req => Grep(ctx, req)), "grep",
                "Search file contents with a .NET regex. Returns file:line: text matches. Optional path, glob filter (e.g. \"*.cs\"), max_results."),

            new Tool(new Func<ListDirRequest, string>(req => ListDir(ctx, req)), "list_dir",
                "List one directory level. Directories end with '/'. Optional path (default: working directory)."),

            new Tool(new Func<ShellRequest, Task<string>>(req => Shell(ctx, req)), "shell",
                "Run a shell command and return stdout+stderr. Optional cwd and timeout_seconds (default 120). " +
                "Not interactive — the command must exit on its own."),
        ];
    }

    // ─────────────────────────────── read_file ───────────────────────────────

    private static string ReadFile(NativeToolContext ctx, ReadFileRequest req)
    {
        string path = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(path))
            return $"ERROR: access denied — '{path}' is outside the allowed directories.";
        if (!File.Exists(path))
            return $"ERROR: file not found: {path}";

        int offset = Math.Max(1, req.Offset ?? 1);
        int limit = Math.Clamp(req.Limit ?? MaxReadLines, 1, MaxReadLines);

        StringBuilder sb = new();
        int lineNumber = 0, emitted = 0;
        bool truncatedByLimit = false;
        foreach (string line in File.ReadLines(path))
        {
            lineNumber++;
            if (lineNumber < offset)
                continue;
            if (emitted >= limit)
            {
                truncatedByLimit = true;
                break;
            }

            string text = line.Length > MaxLineChars ? line[..MaxLineChars] + "…" : line;
            sb.Append(lineNumber).Append(": ").AppendLine(text);
            emitted++;
        }

        if (emitted == 0)
            return lineNumber == 0 ? "(empty file)" : $"ERROR: offset {offset} is past the end of the file ({lineNumber} lines).";
        if (truncatedByLimit)
            sb.AppendLine($"[more lines follow — call again with offset: {offset + emitted}]");
        return sb.ToString();
    }

    // ─────────────────────────────── write_file ───────────────────────────────

    private static string WriteFile(NativeToolContext ctx, WriteFileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Path))
            return "ERROR: 'path' is required.";

        string path = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(path))
            return $"ERROR: access denied — '{path}' is outside the allowed directories.";

        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, req.Content ?? string.Empty);
            return $"Wrote {Encoding.UTF8.GetByteCount(req.Content ?? string.Empty)} bytes to {path}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    // ─────────────────────────────── edit_file ───────────────────────────────

    private static string EditFile(NativeToolContext ctx, EditFileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Path) || string.IsNullOrEmpty(req.OldString))
            return "ERROR: 'path' and 'old_string' are required.";

        string path = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(path))
            return $"ERROR: access denied — '{path}' is outside the allowed directories.";
        if (!File.Exists(path))
            return $"ERROR: file not found: {path}";

        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }

        int matches = CountOccurrences(content, req.OldString);
        if (matches == 0)
            return "ERROR: old_string not found in the file. Read the file and copy the exact text, including whitespace.";
        if (matches > 1 && req.ReplaceAll != true)
            return $"ERROR: old_string is not unique ({matches} matches) — include more surrounding context, or set replace_all: true.";

        string updated = req.ReplaceAll == true
            ? content.Replace(req.OldString, req.NewString ?? string.Empty)
            : ReplaceFirst(content, req.OldString, req.NewString ?? string.Empty);

        try
        {
            File.WriteAllText(path, updated);
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }

        return req.ReplaceAll == true
            ? $"Replaced {matches} occurrence(s) in {path}"
            : $"Replaced 1 occurrence in {path}";
    }

    // ─────────────────────────────── glob ───────────────────────────────

    private static string GlobFiles(NativeToolContext ctx, GlobRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Pattern))
            return "ERROR: 'pattern' is required.";

        string root = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(root))
            return $"ERROR: access denied — '{root}' is outside the allowed directories.";
        if (!Directory.Exists(root))
            return $"ERROR: directory not found: {root}";

        Regex matcher = GlobToRegex(req.Pattern);
        List<string> hits = [];
        bool capped = false;
        foreach (string file in WalkFiles(root, ref capped))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (matcher.IsMatch(relative))
            {
                hits.Add(relative);
                if (hits.Count >= 500)
                    break;
            }
        }

        if (hits.Count == 0)
            return capped ? "No matches (search stopped at the file-walk cap; narrow the path)." : "No matches.";

        StringBuilder sb = new();
        foreach (string hit in hits)
            sb.AppendLine(hit);
        if (hits.Count >= 500)
            sb.AppendLine("[result cap reached — narrow the pattern]");
        else if (capped)
            sb.AppendLine("[file-walk cap reached — some directories were not scanned]");
        return sb.ToString();
    }

    // ─────────────────────────────── grep ───────────────────────────────

    private static string Grep(NativeToolContext ctx, GrepRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Pattern))
            return "ERROR: 'pattern' is required.";

        Regex regex;
        try
        {
            regex = new Regex(req.Pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
        }
        catch (ArgumentException ex)
        {
            return $"ERROR: invalid regex — {ex.Message}";
        }

        string root = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(root))
            return $"ERROR: access denied — '{root}' is outside the allowed directories.";

        Regex? fileFilter = string.IsNullOrWhiteSpace(req.Glob) ? null : GlobToRegex(req.Glob);
        int maxResults = Math.Clamp(req.MaxResults ?? DefaultGrepResults, 1, 1000);

        // A single file is also a valid target.
        IEnumerable<string> files;
        bool capped = false;
        if (File.Exists(root))
        {
            files = [root];
        }
        else if (Directory.Exists(root))
        {
            files = WalkFiles(root, ref capped);
        }
        else
        {
            return $"ERROR: path not found: {root}";
        }

        StringBuilder sb = new();
        int hits = 0;
        foreach (string file in files)
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (fileFilter is not null && !fileFilter.IsMatch(relative) && !fileFilter.IsMatch(Path.GetFileName(file)))
                continue;
            if (LooksBinary(file))
                continue;

            int lineNumber = 0;
            foreach (string line in ReadLinesSafe(file))
            {
                lineNumber++;
                bool isMatch;
                try
                {
                    isMatch = regex.IsMatch(line);
                }
                catch (RegexMatchTimeoutException)
                {
                    return "ERROR: regex timed out — simplify the pattern.";
                }

                if (!isMatch)
                    continue;

                string text = line.Length > MaxLineChars ? line[..MaxLineChars] + "…" : line;
                sb.Append(relative).Append(':').Append(lineNumber).Append(": ").AppendLine(text.TrimEnd());
                if (++hits >= maxResults)
                {
                    sb.AppendLine($"[stopped at {maxResults} matches — narrow the pattern or raise max_results]");
                    return sb.ToString();
                }
            }
        }

        if (hits == 0)
            return capped ? "No matches (search stopped at the file-walk cap; narrow the path)." : "No matches.";
        if (capped)
            sb.AppendLine("[file-walk cap reached — some directories were not scanned]");
        return sb.ToString();
    }

    // ─────────────────────────────── list_dir ───────────────────────────────

    private static string ListDir(NativeToolContext ctx, ListDirRequest req)
    {
        string path = ctx.Resolve(req.Path);
        if (!ctx.Policy.IsFilesystemPathAllowed(path))
            return $"ERROR: access denied — '{path}' is outside the allowed directories.";
        if (!Directory.Exists(path))
            return $"ERROR: directory not found: {path}";

        StringBuilder sb = new();
        foreach (string dir in Directory.EnumerateDirectories(path).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            sb.Append(Path.GetFileName(dir)).AppendLine("/");
        foreach (string file in Directory.EnumerateFiles(path).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine(Path.GetFileName(file));

        return sb.Length == 0 ? "(empty directory)" : sb.ToString();
    }

    // ─────────────────────────────── shell ───────────────────────────────

    private static async Task<string> Shell(NativeToolContext ctx, ShellRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Command))
            return "ERROR: 'command' is required.";

        McpSessionPolicy policy = ctx.Policy;
        if (!policy.IsCommandAllowed(req.Command))
            return $"ERROR: command '{McpSessionPolicy.NormalizeCommandName(req.Command)}' is blocked by session policy.";

        string cwd = ctx.Resolve(req.Cwd);
        if (!policy.IsTerminalDirectoryAllowed(cwd))
            return $"ERROR: working directory '{cwd}' is outside the allowed terminal directories.";
        if (!Directory.Exists(cwd))
            return $"ERROR: working directory not found: {cwd}";

        int timeoutSeconds = Math.Clamp(req.TimeoutSeconds ?? DefaultShellTimeoutSeconds, 1, MaxShellTimeoutSeconds);

        using System.Diagnostics.Process process = new();
        if (OperatingSystem.IsWindows())
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + req.Command;
        }
        else
        {
            process.StartInfo.FileName = "/bin/sh";
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(req.Command);
        }

        process.StartInfo.WorkingDirectory = cwd;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.CreateNoWindow = true;

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return $"ERROR: failed to start command — {ex.Message}";
        }

        process.StandardInput.Close(); // never interactive

        // Async readers avoid the classic full-pipe deadlock.
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            return $"ERROR: command timed out after {timeoutSeconds}s and was killed.";
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        StringBuilder sb = new();
        if (!string.IsNullOrWhiteSpace(stdout))
            sb.AppendLine(stdout.TrimEnd());
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            sb.AppendLine("--- stderr ---");
            sb.AppendLine(stderr.TrimEnd());
        }
        sb.Append("(exit code ").Append(process.ExitCode).Append(')');

        string result = sb.ToString();
        if (result.Length > MaxOutputChars)
            result = result[..MaxOutputChars] + $"\n[output truncated at {MaxOutputChars} chars]";
        return result;
    }

    // ─────────────────────────────── Helpers ───────────────────────────────

    /// <summary>Depth-first file walk skipping ignored directories, capped at <see cref="MaxWalkFiles"/>.</summary>
    private static List<string> WalkFiles(string root, ref bool capped)
    {
        List<string> files = [];
        Stack<string> pending = new();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string dir = pending.Pop();
            try
            {
                foreach (string sub in Directory.EnumerateDirectories(dir))
                {
                    if (!IgnoredDirectories.Contains(Path.GetFileName(sub)))
                        pending.Push(sub);
                }

                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    files.Add(file);
                    if (files.Count >= MaxWalkFiles)
                    {
                        capped = true;
                        return files;
                    }
                }
            }
            catch (UnauthorizedAccessException) { /* skip unreadable directories */ }
            catch (IOException) { /* skip transient errors */ }
        }

        return files;
    }

    /// <summary>Translate a glob (** / * / ?) into an anchored regex over '/'-separated relative paths.</summary>
    internal static Regex GlobToRegex(string pattern)
    {
        string normalized = pattern.Replace('\\', '/');
        StringBuilder regex = new("^");
        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            switch (c)
            {
                case '*':
                    if (i + 1 < normalized.Length && normalized[i + 1] == '*')
                    {
                        // "**/" or trailing "**" — any number of path segments
                        regex.Append(".*");
                        i++;
                        if (i + 1 < normalized.Length && normalized[i + 1] == '/')
                            i++;
                    }
                    else
                    {
                        regex.Append("[^/]*");
                    }
                    break;
                case '?':
                    regex.Append("[^/]");
                    break;
                default:
                    regex.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        regex.Append('$');
        return new Regex(regex.ToString(), RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
    }

    /// <summary>NUL byte in the first 8KB → treat as binary.</summary>
    private static bool LooksBinary(string path)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            Span<byte> buffer = stackalloc byte[8192];
            int read = fs.Read(buffer);
            return buffer[..read].IndexOf((byte)0) >= 0;
        }
        catch
        {
            return true; // unreadable → skip
        }
    }

    private static IEnumerable<string> ReadLinesSafe(string path)
    {
        IEnumerator<string>? enumerator = null;
        try
        {
            enumerator = File.ReadLines(path).GetEnumerator();
        }
        catch
        {
            yield break;
        }

        using (enumerator)
        {
            while (true)
            {
                bool moved;
                try
                {
                    moved = enumerator.MoveNext();
                }
                catch
                {
                    yield break;
                }

                if (!moved)
                    yield break;
                yield return enumerator.Current;
            }
        }
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string ReplaceFirst(string content, string oldValue, string newValue)
    {
        int index = content.IndexOf(oldValue, StringComparison.Ordinal);
        return index < 0 ? content : content[..index] + newValue + content[(index + oldValue.Length)..];
    }
}

// ─────────────────────────────── Request records ───────────────────────────────

public sealed class ReadFileRequest
{
    public string Path { get; set; } = "";
    /// <summary>1-based first line to return.</summary>
    public int? Offset { get; set; }
    /// <summary>Maximum number of lines to return.</summary>
    public int? Limit { get; set; }
}

public sealed class WriteFileRequest
{
    public string Path { get; set; } = "";
    public string? Content { get; set; }
}

public sealed class EditFileRequest
{
    public string Path { get; set; } = "";
    public string OldString { get; set; } = "";
    public string? NewString { get; set; }
    public bool? ReplaceAll { get; set; }
}

public sealed class GlobRequest
{
    public string Pattern { get; set; } = "";
    public string? Path { get; set; }
}

public sealed class GrepRequest
{
    public string Pattern { get; set; } = "";
    public string? Path { get; set; }
    public string? Glob { get; set; }
    public int? MaxResults { get; set; }
}

public sealed class ListDirRequest
{
    public string? Path { get; set; }
}

public sealed class ShellRequest
{
    public string Command { get; set; } = "";
    public string? Cwd { get; set; }
    public int? TimeoutSeconds { get; set; }
}
