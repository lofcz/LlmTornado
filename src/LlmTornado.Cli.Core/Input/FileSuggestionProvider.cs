namespace LlmTornado.Cli.Core.Input;

/// <summary>
/// Provides autocomplete suggestions for <c>@file</c> attachment references.
/// Scans the working directory recursively for attachable file types
/// (the set supported by <see cref="InputParser"/>), skipping common build/VCS
/// directories, and ranks candidates against a typed partial.
/// </summary>
/// <remarks>
/// The full file list is cached per working directory with a short TTL so that
/// filtering on every keystroke does not trigger a fresh disk walk. The cache is
/// invalidated when the working directory changes (e.g. via <c>/cd</c>) or when it
/// becomes older than the TTL.
/// </remarks>
public sealed class FileSuggestionProvider
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "bin", "obj", "node_modules", ".vs", ".idea",
    };

    private readonly TimeSpan _cacheTtl;

    private string? _cachedDirectory;
    private List<string>? _cachedFiles;
    private DateTime _cachedAtUtc;

    public FileSuggestionProvider(TimeSpan? cacheTtl = null)
        => _cacheTtl = cacheTtl ?? TimeSpan.FromSeconds(5);

    /// <summary>
    /// Return up to <paramref name="max"/> attachable files under
    /// <paramref name="workingDirectory"/> whose relative path matches
    /// <paramref name="partial"/> (the text the user typed after <c>@</c>).
    /// Paths are returned relative to the working directory, best matches first.
    /// </summary>
    public IReadOnlyList<string> Suggest(string workingDirectory, string partial, int max = 50)
    {
        IReadOnlyList<string> all = GetFiles(workingDirectory);
        return Rank(all, partial, max);
    }

    private IReadOnlyList<string> GetFiles(string workingDirectory)
    {
        bool sameDir = string.Equals(_cachedDirectory, workingDirectory, StringComparison.OrdinalIgnoreCase);
        bool fresh = (DateTime.UtcNow - _cachedAtUtc) < _cacheTtl;

        if (_cachedFiles is not null && sameDir && fresh)
            return _cachedFiles;

        List<string> files = ScanFiles(workingDirectory);
        _cachedFiles = files;
        _cachedDirectory = workingDirectory;
        _cachedAtUtc = DateTime.UtcNow;
        return files;
    }

    /// <summary>
    /// Recursively enumerate attachable files under <paramref name="workingDirectory"/>,
    /// returning relative paths and skipping ignored directories. Pure (no caching).
    /// </summary>
    public static List<string> ScanFiles(string workingDirectory)
    {
        List<string> results = [];

        if (string.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory))
            return results;

        // Manual stack walk so we can prune ignored directories — EnumerateFiles with
        // AllDirectories cannot skip subtrees.
        Stack<string> pending = new();
        pending.Push(workingDirectory);

        while (pending.Count > 0)
        {
            string dir = pending.Pop();

            string[] subDirs;
            string[] files;
            try
            {
                subDirs = Directory.GetDirectories(dir);
                files = Directory.GetFiles(dir);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file);
                if (!string.IsNullOrEmpty(ext) && InputParser.IsSupportedExtension(ext))
                    results.Add(Path.GetRelativePath(workingDirectory, file));
            }

            foreach (string sub in subDirs)
            {
                string name = Path.GetFileName(sub);
                if (!IgnoredDirectories.Contains(name))
                    pending.Push(sub);
            }
        }

        return results;
    }

    /// <summary>
    /// Filter and rank <paramref name="files"/> against <paramref name="partial"/>.
    /// Path separators are normalized so the user may type either <c>/</c> or <c>\</c>.
    /// Pure and deterministic. When <paramref name="partial"/> is empty, returns the
    /// first <paramref name="max"/> files in alphabetical order.
    /// </summary>
    public static IReadOnlyList<string> Rank(IReadOnlyList<string> files, string partial, int max = 50)
    {
        string partialNorm = Normalize(partial);
        string lastSegment = partialNorm.Contains('/')
            ? partialNorm[(partialNorm.LastIndexOf('/') + 1)..]
            : partialNorm;

        List<(string path, int tier)> scored = [];

        foreach (string path in files)
        {
            string candidate = Normalize(path);
            string fileName = Normalize(Path.GetFileName(path));

            int tier;
            if (partialNorm.Length == 0)
                tier = 0;
            else if (candidate.StartsWith(partialNorm, StringComparison.OrdinalIgnoreCase))
                tier = 0;
            else if (lastSegment.Length > 0 && fileName.StartsWith(lastSegment, StringComparison.OrdinalIgnoreCase))
                tier = 1;
            else if (candidate.Contains(partialNorm, StringComparison.OrdinalIgnoreCase))
                tier = 2;
            else if (lastSegment.Length > 0 && fileName.Contains(lastSegment, StringComparison.OrdinalIgnoreCase))
                tier = 3;
            else
                continue;

            scored.Add((path, tier));
        }

        return scored
            .OrderBy(s => s.tier)
            .ThenBy(s => s.path.Length)
            .ThenBy(s => s.path, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(s => s.path)
            .ToList();
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
