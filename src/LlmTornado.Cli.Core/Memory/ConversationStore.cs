using System.Text.Json;
using System.Text.Json.Serialization;
using LlmTornado.Agents;
using LlmTornado.Chat;

namespace LlmTornado.Cli.Core.Memory;

/// <summary>
/// Metadata for a saved conversation.
/// </summary>
public sealed class ConversationMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("first_message_preview")]
    public string? FirstMessagePreview { get; set; }

    [JsonPropertyName("active_skills")]
    public List<string> ActiveSkills { get; set; } = [];
}

/// <summary>
/// Save, load, list, and delete named conversations.
/// </summary>
[Obsolete("Use SqliteConversationStore from LlmTornado.Cli.Core.Storage instead.")]
public sealed class ConversationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _conversationsDirectory;

    public ConversationStore(string conversationsDirectory)
    {
        _conversationsDirectory = conversationsDirectory;
    }

    /// <summary>
    /// Save the current conversation with a generated or specified label.
    /// </summary>
    public string Save(List<ChatMessage> messages, string? model, List<string>? activeSkills, string? label = null, string? existingId = null)
    {
        string id;
        DateTime createdAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(existingId))
        {
            id = existingId;
            string existingMetaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");
            ConversationMetadata? existingMeta = LoadJson<ConversationMetadata>(existingMetaPath);
            if (existingMeta is not null)
            {
                createdAt = existingMeta.CreatedAt;
                if (label == null) label = existingMeta.Label;
            }
        }
        else
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string slug = label is not null ? "_" + Slugify(label) : "";
            id = $"{timestamp}{slug}";
        }

        string jsonlPath = Path.Combine(_conversationsDirectory, $"{id}.jsonl");
        string metaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");

        if (File.Exists(jsonlPath)) File.Delete(jsonlPath);

        // Write messages as JSONL
        PersistentConversation pc = new(jsonlPath, continuousSave: true);
        foreach (ChatMessage msg in messages)
            pc.AppendMessage(msg);

        // Write metadata
        string? preview = messages.FirstOrDefault(m => m.Role == LlmTornado.Code.ChatMessageRoles.User)?.Content;
        if (preview is not null && preview.Length > 100)
            preview = preview[..100] + "...";

        ConversationMetadata meta = new()
        {
            Id = id,
            Label = label,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow,
            Model = model,
            MessageCount = messages.Count,
            FirstMessagePreview = preview,
            ActiveSkills = activeSkills ?? [],
        };

        SaveJson(metaPath, meta);
        return id;
    }

    /// <summary>
    /// List all saved conversations.
    /// </summary>
    public List<ConversationMetadata> List()
    {
        List<ConversationMetadata> result = [];

        if (!Directory.Exists(_conversationsDirectory))
            return result;

        foreach (string metaFile in Directory.GetFiles(_conversationsDirectory, "*.meta.json"))
        {
            ConversationMetadata? meta = LoadJson<ConversationMetadata>(metaFile);
            if (meta is not null)
                result.Add(meta);
        }

        return [.. result.OrderByDescending(m => m.CreatedAt)];
    }

    /// <summary>
    /// Load a saved conversation by ID.
    /// </summary>
    public List<ChatMessage>? Load(string id)
    {
        string jsonlPath = Path.Combine(_conversationsDirectory, $"{id}.jsonl");
        if (!File.Exists(jsonlPath))
            return null;

        try
        {
            PersistentConversation pc = new(jsonlPath);
            return pc.GetMessages();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete a saved conversation.
    /// </summary>
    public bool Delete(string id)
    {
        string jsonlPath = Path.Combine(_conversationsDirectory, $"{id}.jsonl");
        string metaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");

        bool deleted = false;
        if (File.Exists(jsonlPath)) { File.Delete(jsonlPath); deleted = true; }
        if (File.Exists(metaPath)) { File.Delete(metaPath); deleted = true; }
        return deleted;
    }

    private static string Slugify(string text)
    {
        return string.Join("-", text.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .Select(c => c == ' ' ? '-' : c)
            .ToArray()
            .ToString()!
            .Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static T? LoadJson<T>(string path) where T : class
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

    private static void SaveJson<T>(string path, T data)
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
