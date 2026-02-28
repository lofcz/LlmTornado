using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Core.Input;

/// <summary>
/// The media category of a detected file reference.
/// </summary>
public enum FileMediaType
{
    Image,
    Document,
    Audio
}

/// <summary>
/// A file reference detected in user input.
/// </summary>
public sealed class ParsedFileReference
{
    /// <summary>
    /// The original token as it appeared in the input (e.g., <c>@"./photo.png"</c>).
    /// </summary>
    public required string RawToken { get; init; }

    /// <summary>
    /// The resolved absolute file path.
    /// </summary>
    public required string FilePath { get; init; }
    
    /// <summary>
    /// The file name portion (e.g., <c>photo.png</c>).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The detected media type category.
    /// </summary>
    public required FileMediaType MediaType { get; init; }

    /// <summary>
    /// The MIME type resolved from the extension (e.g., <c>image/png</c>).
    /// </summary>
    public required string MimeType { get; init; }
}

/// <summary>
/// Result of parsing user input for inline file references.
/// </summary>
public sealed class ParsedInput
{
    /// <summary>
    /// The user's text with all <c>@path</c> tokens removed and whitespace cleaned up.
    /// </summary>
    public required string CleanedText { get; init; }

    /// <summary>
    /// File references detected in the input, in order of appearance.
    /// </summary>
    public required List<ParsedFileReference> Files { get; init; }

    /// <summary>
    /// Whether any file references were found.
    /// </summary>
    public bool HasFiles => Files.Count > 0;
}

/// <summary>
/// Scans user input for inline <c>@path/to/file</c> references and resolves them
/// against the file system.
/// </summary>
public static class InputParser
{
    // Matches @"quoted path" or @unquoted/path (no spaces unless quoted)
    // Group 1 = quoted path, Group 2 = unquoted path
    private static readonly Regex FileRefPattern = new(
        """@"([^"]+)"|@(\S+)""",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, FileMediaType> ExtensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".png"] = FileMediaType.Image,
        [".jpg"] = FileMediaType.Image,
        [".jpeg"] = FileMediaType.Image,
        [".gif"] = FileMediaType.Image,
        [".webp"] = FileMediaType.Image,
        [".bmp"] = FileMediaType.Image,
        [".svg"] = FileMediaType.Image,
        [".tiff"] = FileMediaType.Image,
        [".tif"] = FileMediaType.Image,
        [".ico"] = FileMediaType.Image,

        // Documents
        [".pdf"] = FileMediaType.Document,

        // Audio
        [".wav"] = FileMediaType.Audio,
        [".mp3"] = FileMediaType.Audio,
        [".ogg"] = FileMediaType.Audio,
        [".flac"] = FileMediaType.Audio,
        [".m4a"] = FileMediaType.Audio,
        [".aac"] = FileMediaType.Audio,
        [".wma"] = FileMediaType.Audio,
        [".webm"] = FileMediaType.Audio,
    };

    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",
        [".svg"] = "image/svg+xml",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".ico"] = "image/x-icon",
        [".pdf"] = "application/pdf",
        [".wav"] = "audio/wav",
        [".mp3"] = "audio/mpeg",
        [".ogg"] = "audio/ogg",
        [".flac"] = "audio/flac",
        [".m4a"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".wma"] = "audio/x-ms-wma",
        [".webm"] = "audio/webm",
    };

    /// <summary>
    /// The maximum file size allowed for attachments (20 MB).
    /// </summary>
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;

    /// <summary>
    /// Parse user input, extracting any <c>@path</c> file references.
    /// Paths are resolved relative to <paramref name="workingDirectory"/>.
    /// </summary>
    /// <param name="input">Raw user input text.</param>
    /// <param name="workingDirectory">The current working directory for resolving relative paths.</param>
    /// <returns>Parsed result with cleaned text and file references.</returns>
    public static ParsedInput Parse(string input, string workingDirectory)
    {
        MatchCollection matches = FileRefPattern.Matches(input);

        if (matches.Count == 0)
        {
            return new ParsedInput
            {
                CleanedText = input,
                Files = []
            };
        }

        List<ParsedFileReference> files = [];
        List<string> errors = [];
        string cleaned = input;

        // Process matches in reverse order so string indices stay valid during removal
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            string rawToken = match.Value;
            string filePath = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

            // Resolve to absolute path
            string absolutePath;
            try
            {
                absolutePath = Path.IsPathRooted(filePath)
                    ? Path.GetFullPath(filePath)
                    : Path.GetFullPath(Path.Combine(workingDirectory, filePath));
            }
            catch
            {
                errors.Add($"Invalid path: {filePath}");
                continue;
            }

            // Validate file exists
            if (!File.Exists(absolutePath))
            {
                errors.Add($"File not found: {filePath}");
                continue;
            }

            // Check extension
            string ext = Path.GetExtension(absolutePath);
            if (string.IsNullOrEmpty(ext) || !ExtensionToMediaType.TryGetValue(ext, out FileMediaType mediaType))
            {
                errors.Add($"Unsupported file type '{ext}': {filePath}");
                continue;
            }

            // Check file size
            FileInfo fi = new(absolutePath);
            if (fi.Length > MaxFileSizeBytes)
            {
                errors.Add($"File too large ({fi.Length / (1024 * 1024):F1} MB, max {MaxFileSizeBytes / (1024 * 1024)} MB): {filePath}");
                continue;
            }

            string mimeType = ExtensionToMime.GetValueOrDefault(ext, "application/octet-stream");

            files.Add(new ParsedFileReference
            {
                RawToken = rawToken,
                FilePath = absolutePath,
                FileName = Path.GetFileName(absolutePath),
                MediaType = mediaType,
                MimeType = mimeType,
            });

            // Remove the token from the cleaned text
            cleaned = cleaned.Remove(match.Index, match.Length);
        }

        // Reverse files to restore original order (we iterated in reverse)
        files.Reverse();

        // Collapse multiple spaces left by token removal
        cleaned = Regex.Replace(cleaned, @"  +", " ").Trim();

        return new ParsedInput
        {
            CleanedText = cleaned,
            Files = files,
        };
    }

    /// <summary>
    /// Get a friendly list of supported file extensions grouped by media type.
    /// </summary>
    public static string GetSupportedFormatsHelp()
    {
        var grouped = ExtensionToMediaType
            .GroupBy(x => x.Value)
            .OrderBy(g => g.Key);

        List<string> lines = [];
        foreach (var group in grouped)
        {
            string extensions = string.Join(", ", group.Select(x => x.Key));
            lines.Add($"  {group.Key}: {extensions}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Check whether a given extension is a supported media type.
    /// </summary>
    public static bool IsSupportedExtension(string extension)
    {
        return ExtensionToMediaType.ContainsKey(extension);
    }
}
