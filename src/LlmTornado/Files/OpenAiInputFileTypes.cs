using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using LlmTornado.Code.MimeTypeMap;

namespace LlmTornado.Files;

/// <summary>
/// Category of files accepted by OpenAI <c>input_file</c> (Feb 24, 2026 expansion).
/// See <see href="https://developers.openai.com/api/docs/guides/pdf-files"/>.
/// </summary>
public enum OpenAiInputFileCategory
{
    /// <summary>PDF documents (<c>application/pdf</c>).</summary>
    Pdf,

    /// <summary>Spreadsheets (Excel, CSV, TSV, IIF, Google Sheets).</summary>
    Spreadsheet,

    /// <summary>Rich documents (Word, ODT, RTF, Pages, Google Docs).</summary>
    RichDocument,

    /// <summary>Presentations (PowerPoint, Keynote, Google Slides).</summary>
    Presentation,

    /// <summary>Plain text, markup, and source code files.</summary>
    TextAndCode
}

/// <summary>
/// MIME types and extensions accepted for OpenAI <c>input_file</c> in Chat and Responses APIs
/// (expanded Feb 24, 2026). Provider validation applies to OpenAI uploads with <see cref="FilePurpose.UserData"/>.
/// </summary>
public static class OpenAiInputFileTypes
{
    /// <summary>Maximum combined size of all files in a single request (50 MB).</summary>
    public const long MaxRequestBytes = 50 * 1024 * 1024;

    private static readonly HashSet<string> SupportedMimeTypes = CreateMimeSet();
    private static readonly HashSet<string> SupportedExtensions = CreateExtensionSet();
    private static readonly FrozenDictionary<string, OpenAiInputFileCategory> ExtensionCategories = CreateExtensionCategories().ToFrozenDictionary();

    /// <summary>All supported MIME types (lowercase).</summary>
    public static IReadOnlyCollection<string> MimeTypes => SupportedMimeTypes;

    /// <summary>All supported file extensions without leading dot (lowercase).</summary>
    public static IReadOnlyCollection<string> Extensions => SupportedExtensions;

    /// <summary>
    /// Returns whether <paramref name="mimeType"/> is an accepted <c>input_file</c> MIME type.
    /// </summary>
    public static bool IsSupportedMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return false;
        }

        int semi = mimeType.IndexOf(';');
        string normalized = (semi >= 0 ? mimeType[..semi] : mimeType).Trim().ToLowerInvariant();
        return SupportedMimeTypes.Contains(normalized);
    }

    /// <summary>
    /// Returns whether the file extension (with or without leading dot) is accepted for <c>input_file</c>.
    /// </summary>
    public static bool IsSupportedExtension(string? extensionOrFileName)
    {
        if (string.IsNullOrWhiteSpace(extensionOrFileName))
        {
            return false;
        }

        return SupportedExtensions.Contains(NormalizeExtension(extensionOrFileName));
    }

    /// <summary>
    /// Resolves the MIME type for a filename using extension mapping, then checks <see cref="IsSupportedMimeType"/>.
    /// </summary>
    public static bool TryResolveMimeType(string fileName, out string mimeType)
    {
        mimeType = MimeTypeMap.GetMimeType(fileName);
        return IsSupportedMimeType(mimeType);
    }

    /// <summary>
    /// Gets the <see cref="OpenAiInputFileCategory"/> for a filename extension, if supported.
    /// </summary>
    public static bool TryGetCategory(string fileName, out OpenAiInputFileCategory category)
    {
        category = default;
        string ext = NormalizeExtension(fileName);
        return ExtensionCategories.TryGetValue(ext, out category);
    }

    /// <summary>
    /// Validates filename and optional MIME type for OpenAI <c>input_file</c> usage.
    /// </summary>
    public static bool TryValidate(string fileName, string? mimeType, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errorMessage = "A filename is required for input_file.";
            return false;
        }

        string ext = NormalizeExtension(fileName);

        if (!SupportedExtensions.Contains(ext))
        {
            errorMessage = $"File extension '.{ext}' is not supported for OpenAI input_file. See OpenAiInputFileTypes for accepted types.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(mimeType) && !IsSupportedMimeType(mimeType))
        {
            errorMessage = $"MIME type '{mimeType}' is not supported for OpenAI input_file.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(mimeType))
        {
            return true;
        }

        if (!TryResolveMimeType(fileName, out _))
        {
            errorMessage = $"Could not resolve a supported MIME type for '{fileName}'.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates and throws <see cref="ArgumentException"/> when the file is not supported.
    /// </summary>
    public static void ValidateOrThrow(string fileName, string? mimeType = null)
    {
        if (!TryValidate(fileName, mimeType, out string? error))
        {
            throw new ArgumentException(error, nameof(fileName));
        }
    }

    private static string NormalizeExtension(string extensionOrFileName)
    {
        string s = extensionOrFileName.Trim();
        int dot = s.LastIndexOf('.');
        if (dot >= 0 && dot < s.Length - 1)
        {
            s = s[(dot + 1)..];
        }

        return s.TrimStart('.').ToLowerInvariant();
    }

    private static HashSet<string> CreateMimeSet()
    {
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // PDF
            "application/pdf",

            // Spreadsheets — Excel
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",

            // Spreadsheets — CSV / TSV / IIF / Google Sheets
            "text/csv",
            "application/csv",
            "text/tsv",
            "text/x-iif",
            "application/x-iif",
            "application/vnd.google-apps.spreadsheet",

            // Rich documents
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword",
            "application/rtf",
            "text/rtf",
            "application/vnd.oasis.opendocument.text",
            "application/vnd.apple.pages",
            "application/vnd.google-apps.document",
            "application/vnd.apple.iwork",

            // Presentations
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.ms-powerpoint",
            "application/vnd.apple.keynote",
            "application/vnd.google-apps.presentation",

            // Text and code
            "application/javascript",
            "application/typescript",
            "text/xml",
            "text/x-shellscript",
            "text/x-rst",
            "text/x-makefile",
            "text/x-lisp",
            "text/x-asm",
            "text/vbscript",
            "text/css",
            "message/rfc822",
            "application/x-sql",
            "application/x-scala",
            "application/x-rust",
            "application/x-powershell",
            "text/x-diff",
            "text/x-patch",
            "application/x-patch",
            "text/plain",
            "text/markdown",
            "text/x-java",
            "text/x-script.python",
            "text/x-python",
            "text/x-c",
            "text/x-c++",
            "text/x-golang",
            "text/html",
            "text/x-php",
            "application/x-php",
            "application/x-httpd-php",
            "application/x-httpd-php-source",
            "text/x-ruby",
            "text/x-sh",
            "text/x-bash",
            "application/x-bash",
            "text/x-zsh",
            "text/x-tex",
            "text/x-csharp",
            "application/json",
            "text/x-typescript",
            "text/javascript",
            "text/x-go",
            "text/x-rust",
            "text/x-scala",
            "text/x-kotlin",
            "text/x-swift",
            "text/x-lua",
            "text/x-r",
            "text/x-R",
            "text/x-julia",
            "text/x-perl",
            "text/x-objectivec",
            "text/x-objectivec++",
            "text/x-erlang",
            "text/x-elixir",
            "text/x-haskell",
            "text/x-clojure",
            "text/x-groovy",
            "text/x-dart",
            "text/x-awk",
            "application/x-awk",
            "text/jsx",
            "text/tsx",
            "text/x-handlebars",
            "text/x-mustache",
            "text/x-ejs",
            "text/x-jinja2",
            "text/x-liquid",
            "text/x-erb",
            "text/x-twig",
            "text/x-pug",
            "text/x-jade",
            "text/x-tmpl",
            "text/x-cmake",
            "text/x-dockerfile",
            "text/x-gradle",
            "text/x-ini",
            "text/x-properties",
            "text/x-protobuf",
            "application/x-protobuf",
            "text/x-sql",
            "text/x-sass",
            "text/x-scss",
            "text/x-less",
            "text/x-hcl",
            "text/x-terraform",
            "application/x-terraform",
            "text/x-toml",
            "application/x-toml",
            "application/graphql",
            "application/x-graphql",
            "text/x-graphql",
            "application/x-ndjson",
            "application/json5",
            "application/x-json5",
            "text/x-yaml",
            "application/toml",
            "application/x-yaml",
            "application/yaml",
            "text/x-astro",
            "text/srt",
            "application/x-subrip",
            "text/x-subrip",
            "text/vtt",
            "text/x-vcard",
            "text/calendar"
        };

        return set;
    }

    private static HashSet<string> CreateExtensionSet()
    {
        HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddRange(IEnumerable<string> extensions)
        {
            foreach (string ext in extensions)
            {
                set.Add(ext.TrimStart('.').ToLowerInvariant());
            }
        }

        AddRange(["pdf"]);
        AddRange(["xla", "xlb", "xlc", "xlm", "xls", "xlsx", "xlt", "xlw", "csv", "tsv", "iif"]);
        AddRange(["doc", "docx", "dot", "odt", "rtf"]);
        AddRange(["pot", "ppa", "pps", "ppt", "pptx", "pwz", "wiz"]);
        AddRange([
            "asm", "bat", "c", "cc", "conf", "cpp", "css", "cxx", "def", "dic", "eml", "h", "hh", "htm", "html",
            "ics", "ifb", "in", "js", "json", "ksh", "list", "log", "markdown", "md", "mht", "mhtml", "mime", "mjs",
            "nws", "pl", "py", "rst", "s", "sql", "srt", "text", "txt", "vcf", "vtt", "xml"
        ]);

        return set;
    }

    private static Dictionary<string, OpenAiInputFileCategory> CreateExtensionCategories()
    {
        Dictionary<string, OpenAiInputFileCategory> map = new Dictionary<string, OpenAiInputFileCategory>(StringComparer.OrdinalIgnoreCase);

        void Add(OpenAiInputFileCategory category, params string[] extensions)
        {
            foreach (string ext in extensions)
            {
                map[ext.TrimStart('.').ToLowerInvariant()] = category;
            }
        }

        Add(OpenAiInputFileCategory.Pdf, "pdf");
        Add(OpenAiInputFileCategory.Spreadsheet, "xla", "xlb", "xlc", "xlm", "xls", "xlsx", "xlt", "xlw", "csv", "tsv", "iif");
        Add(OpenAiInputFileCategory.RichDocument, "doc", "docx", "dot", "odt", "rtf");
        Add(OpenAiInputFileCategory.Presentation, "pot", "ppa", "pps", "ppt", "pptx", "pwz", "wiz");
        Add(OpenAiInputFileCategory.TextAndCode,
            "asm", "bat", "c", "cc", "conf", "cpp", "css", "cxx", "def", "dic", "eml", "h", "hh", "htm", "html",
            "ics", "ifb", "in", "js", "json", "ksh", "list", "log", "markdown", "md", "mht", "mhtml", "mime", "mjs",
            "nws", "pl", "py", "rst", "s", "sql", "srt", "text", "txt", "vcf", "vtt", "xml");

        return map;
    }
}
