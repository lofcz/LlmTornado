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

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

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
    /// Save a conversation with a caller-provided ID (e.g. the client session GUID).
    /// Use this overload when the caller owns the ID lifecycle.
    /// </summary>
    public void Save(string id, List<ChatMessage> messages, string? userId, string? model, List<string>? activeSkills, string? label = null)
    {
        WriteConversation(id, messages, userId, model, activeSkills, label, isNew: true);
    }

    /// <summary>
    /// Save the current conversation with an auto-generated timestamp-based ID.
    /// Returns the generated ID.
    /// </summary>
    public string Save(List<ChatMessage> messages, string? model, List<string>? activeSkills, string? label = null, string? userId = null)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string slug = label is not null ? "_" + Slugify(label) : "";
        string id = $"{timestamp}{slug}";

        WriteConversation(id, messages, userId, model, activeSkills, label, isNew: true);
        return id;
    }

    /// <summary>
    /// Update an existing conversation on disk (messages + metadata).
    /// Returns false if the conversation does not exist.
    /// </summary>
    public bool Update(string id, List<ChatMessage> messages, string? model = null)
    {
        string metaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");
        if (!File.Exists(metaPath))
            return false;

        // Load existing metadata to preserve fields like label, userId, activeSkills
        ConversationMetadata? existing = LoadJson<ConversationMetadata>(metaPath);
        if (existing is null)
            return false;

        // Rewrite the JSONL with full current message list (atomic via tmp)
        string jsonlPath = Path.Combine(_conversationsDirectory, $"{id}.jsonl");
        string tmpJsonl = jsonlPath + ".tmp";

        // Delete any existing tmp file and write fresh
        if (File.Exists(tmpJsonl)) File.Delete(tmpJsonl);
        PersistentConversation pc = new(tmpJsonl, continuousSave: true);
        foreach (ChatMessage msg in messages)
            pc.AppendMessage(msg);
        File.Move(tmpJsonl, jsonlPath, overwrite: true);

        // Update metadata
        string? preview = existing.FirstMessagePreview ?? GetPreview(messages);

        existing.UpdatedAt = DateTime.UtcNow;
        existing.MessageCount = messages.Count;
        existing.FirstMessagePreview = preview;
        if (model is not null)
            existing.Model = model;

        SaveJson(metaPath, existing);
        return true;
    }

    /// <summary>
    /// List all saved conversations, optionally filtered by user ID.
    /// </summary>
    public List<ConversationMetadata> List(string? userId = null)
    {
        List<ConversationMetadata> result = [];

        if (!Directory.Exists(_conversationsDirectory))
            return result;

        foreach (string metaFile in Directory.GetFiles(_conversationsDirectory, "*.meta.json"))
        {
            ConversationMetadata? meta = LoadJson<ConversationMetadata>(metaFile);
            if (meta is null) continue;

            if (userId is not null && !string.Equals(meta.UserId, userId, StringComparison.Ordinal))
                continue;

            result.Add(meta);
        }

        return [.. result.OrderByDescending(m => m.CreatedAt)];
    }

    /// <summary>
    /// Check whether a conversation with the given ID exists on disk.
    /// </summary>
    public bool Exists(string id)
    {
        string metaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");
        return File.Exists(metaPath);
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
        catch (Exception ex)
        {
            // Log enough context to diagnose corrupt JSONL files
            System.Diagnostics.Debug.WriteLine($"ConversationStore.Load failed for '{id}': {ex.Message}");
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

    // ──────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────

    private void WriteConversation(string id, List<ChatMessage> messages, string? userId, string? model, List<string>? activeSkills, string? label, bool isNew)
    {
        string jsonlPath = Path.Combine(_conversationsDirectory, $"{id}.jsonl");
        string metaPath = Path.Combine(_conversationsDirectory, $"{id}.meta.json");

        // Write messages as JSONL
        PersistentConversation pc = new(jsonlPath, continuousSave: true);
        foreach (ChatMessage msg in messages)
            pc.AppendMessage(msg);

        // Write metadata
        string? preview = GetPreview(messages);
        DateTime now = DateTime.UtcNow;

        ConversationMetadata meta = new()
        {
            Id = id,
            Label = label,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            Model = model,
            MessageCount = messages.Count,
            FirstMessagePreview = preview,
            ActiveSkills = activeSkills ?? [],
        };

        SaveJson(metaPath, meta);
    }

    private static string? GetPreview(List<ChatMessage> messages)
    {
        string? preview = messages.FirstOrDefault(m => m.Role == LlmTornado.Code.ChatMessageRoles.User)?.Content;
        if (preview is not null && preview.Length > 100)
            preview = preview[..100] + "...";
        return preview;
    }

    internal static string Slugify(string text)
    {
        return string.Join("-", new string(text.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .Select(c => c == ' ' ? '-' : c)
            .ToArray())
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConversationStore.LoadJson failed for '{path}': {ex.Message}");
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
