using System.Text.Json;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli;

/// <summary>
/// Manages the persistent data directory for the CLI agent.
/// </summary>
internal static class CliStorage
{
    private static string? _rootDirectory;

    public static string RootDirectory => _rootDirectory ?? ResolveRootDirectory();
    public static string ConversationsDirectory => Path.Combine(RootDirectory, "conversations");
    public static string SettingsPath => Path.Combine(RootDirectory, "settings.json");
    public static string ToolApprovalsPath => Path.Combine(RootDirectory, "tool-approvals.json");
    public static string CurrentConversationPath => Path.Combine(ConversationsDirectory, "current.jsonl");
    public static string DatabasePath => Path.Combine(RootDirectory, "conversations.db");
    public static string AttachmentsDirectory => Path.Combine(RootDirectory, "attachments");
    public static string ContextDumpsDirectory => Path.Combine(RootDirectory, "context-dumps");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Bind storage to the shared CLI root for this run and ensure all directories exist.
    /// </summary>
    public static void Initialize()
    {
        _rootDirectory = ResolveRootDirectory();

        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ConversationsDirectory);
        Directory.CreateDirectory(ContextDumpsDirectory);
    }

    internal static string ResolveRootDirectory()
        => TornadoPaths.GlobalRoot();

    internal static void ResetForTesting()
        => _rootDirectory = null;

    /// <summary>
    /// Read and deserialize a JSON file, or return null if not found.
    /// </summary>
    public static T? LoadJson<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Atomically serialize and write a JSON file.
    /// </summary>
    public static void SaveJson<T>(string path, T data)
    {
        string? dir = Path.GetDirectoryName(path);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(data, JsonOptions);
        string tmpPath = path + ".tmp";

        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, path, overwrite: true);
    }
}
