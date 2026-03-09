using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Cli.Core.Memory;

namespace LlmTornado.Cli.Core.Storage;

/// <summary>
/// Migrates existing file-based conversations (JSONL + meta.json) into the SQLite store.
/// Run once on first startup with the new store.
/// </summary>
public static class ConversationMigrator
{
    /// <summary>
    /// Import all conversations from a file-based ConversationStore into SQLite.
    /// Returns the count of successfully migrated conversations.
    /// </summary>
    public static int MigrateAll(string conversationsDirectory, SqliteConversationStore store)
    {
        if (!Directory.Exists(conversationsDirectory))
            return 0;

        string[] metaFiles = Directory.GetFiles(conversationsDirectory, "*.meta.json");
        int migrated = 0;

        foreach (string metaFile in metaFiles)
        {
            try
            {
                string id = Path.GetFileName(metaFile).Replace(".meta.json", "");
                string jsonlPath = Path.Combine(conversationsDirectory, $"{id}.jsonl");

                if (!File.Exists(jsonlPath))
                    continue;

                // Check if already migrated (conversation exists in DB)
                List<ConversationMetadata> existing = store.List();
                if (existing.Any(c => c.Id == id))
                    continue;

                // Load messages from JSONL
                PersistentConversation pc = new(jsonlPath);
                List<ChatMessage> messages = pc.GetMessages();

                if (messages.Count == 0)
                    continue;

                // Load metadata
                string metaJson = File.ReadAllText(metaFile);
                ConversationMetadata? meta = System.Text.Json.JsonSerializer.Deserialize<ConversationMetadata>(metaJson);

                // Save into SQLite with the same ID
                store.Save(
                    messages,
                    meta?.Model,
                    meta?.ActiveSkills,
                    meta?.Label,
                    existingId: id);

                migrated++;
            }
            catch
            {
                // Skip broken conversations
            }
        }

        return migrated;
    }

    /// <summary>
    /// Migrate the current active conversation file (current.jsonl) into the store.
    /// Returns the conversation ID if migrated, null otherwise.
    /// </summary>
    public static string? MigrateCurrent(string currentJsonlPath, SqliteConversationStore store)
    {
        if (!File.Exists(currentJsonlPath))
            return null;

        try
        {
            PersistentConversation pc = new(currentJsonlPath);
            List<ChatMessage> messages = pc.GetMessages();

            if (messages.Count == 0)
                return null;

            string id = store.Save(messages, null, null, label: "migrated-current");
            return id;
        }
        catch
        {
            return null;
        }
    }
}
