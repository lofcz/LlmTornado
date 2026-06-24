using System.Text.Json;

namespace LlmTornado.Cli;

/// <summary>
/// Manages the persistent data directory for the CLI agent.
/// </summary>
internal static class CliStorage
{
    public static readonly string RootDirectory = OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlmTornado")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".llmtornado");

    public static readonly string ConversationsDirectory = Path.Combine(RootDirectory, "conversations");
    public static readonly string SettingsPath = Path.Combine(RootDirectory, "settings.json");
    public static readonly string ToolApprovalsPath = Path.Combine(RootDirectory, "tool-approvals.json");
    public static readonly string CurrentConversationPath = Path.Combine(ConversationsDirectory, "current.jsonl");
    public static readonly string DatabasePath = Path.Combine(RootDirectory, "conversations.db");
    public static readonly string AttachmentsDirectory = Path.Combine(RootDirectory, "attachments");
    public static readonly string ContextDumpsDirectory = Path.Combine(RootDirectory, "context-dumps");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Ensure all directories exist.
    /// </summary>
    public static void Initialize()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ConversationsDirectory);
        Directory.CreateDirectory(ContextDumpsDirectory);
    }

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
