using System.Text.Json;
using LlmTornado.Chat;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Code;
using Microsoft.Data.Sqlite;

namespace LlmTornado.Cli.Core.Storage;

/// <summary>
/// SQLite-backed conversation store. Replaces the file-based <see cref="ConversationStore"/>
/// with structured storage, full message history, attachment externalization, and restore points.
/// </summary>
public sealed class SqliteConversationStore : IDisposable
{
    private readonly ConversationDatabase _db;
    private readonly AttachmentStore _attachments;

    public SqliteConversationStore(string databasePath, string? attachmentsDirectory = null)
    {
        _db = new ConversationDatabase(databasePath);
        string attachDir = attachmentsDirectory ?? Path.Combine(_db.DatabaseDirectory, "attachments");
        _attachments = new AttachmentStore(attachDir);
    }

    public SqliteConversationStore(ConversationDatabase db, AttachmentStore attachments)
    {
        _db = db;
        _attachments = attachments;
    }

    /// <summary>
    /// The underlying attachment store, exposed for lazy-load operations.
    /// </summary>
    public AttachmentStore Attachments => _attachments;

    // ───────────────────────────────────────────────
    // Save
    // ───────────────────────────────────────────────

    /// <summary>
    /// Save or update a conversation. Messages are upserted, attachments extracted to disk.
    /// Returns the conversation ID.
    /// </summary>
    public string Save(
        List<ChatMessage> messages,
        string? model,
        List<string>? activeSkills,
        string? label = null,
        string? existingId = null,
        string? activeAgent = null)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteTransaction tx = conn.BeginTransaction();

        try
        {
            string id;
            DateTime createdAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(existingId))
            {
                id = existingId;
                // Preserve original creation time
                using SqliteCommand getCreated = conn.CreateCommand();
                getCreated.Transaction = tx;
                getCreated.CommandText = "SELECT created_at FROM conversations WHERE id = @id";
                getCreated.Parameters.AddWithValue("@id", id);
                string? existingCreated = getCreated.ExecuteScalar() as string;
                if (existingCreated is not null && DateTime.TryParse(existingCreated, out DateTime parsed))
                    createdAt = parsed;

                // Preserve label if not explicitly set
                if (label is null)
                {
                    using SqliteCommand getLabel = conn.CreateCommand();
                    getLabel.Transaction = tx;
                    getLabel.CommandText = "SELECT label FROM conversations WHERE id = @id";
                    getLabel.Parameters.AddWithValue("@id", id);
                    label = getLabel.ExecuteScalar() as string;
                }

                // Preserve the raw transcript rows; only reset which rows make up the active model context.
                HideMessages(conn, tx, id);
            }
            else
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string slug = label is not null ? "_" + Slugify(label) : "";
                id = $"{timestamp}{slug}";
            }

            // Extract first user message preview
            string? preview = messages.FirstOrDefault(m => m.Role == ChatMessageRoles.User)?.Content;
            if (preview is not null && preview.Length > 100)
                preview = preview[..100] + "...";

            // Upsert conversation
            using SqliteCommand upsertConv = conn.CreateCommand();
            upsertConv.Transaction = tx;
            upsertConv.CommandText = """
                INSERT INTO conversations (id, label, created_at, updated_at, model, active_agent, active_skills, message_count, first_preview)
                VALUES (@id, @label, @created_at, @updated_at, @model, @agent, @skills, @count, @preview)
                ON CONFLICT(id) DO UPDATE SET
                    label = @label,
                    updated_at = @updated_at,
                    model = @model,
                    active_agent = @agent,
                    active_skills = @skills,
                    message_count = @count,
                    first_preview = @preview
                """;
            upsertConv.Parameters.AddWithValue("@id", id);
            upsertConv.Parameters.AddWithValue("@label", (object?)label ?? DBNull.Value);
            upsertConv.Parameters.AddWithValue("@created_at", createdAt.ToString("O"));
            upsertConv.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.ToString("O"));
            upsertConv.Parameters.AddWithValue("@model", (object?)model ?? DBNull.Value);
            upsertConv.Parameters.AddWithValue("@agent", (object?)activeAgent ?? DBNull.Value);
            upsertConv.Parameters.AddWithValue("@skills", activeSkills is { Count: > 0 } ? JsonSerializer.Serialize(activeSkills) : DBNull.Value);
            upsertConv.Parameters.AddWithValue("@count", messages.Count);
            upsertConv.Parameters.AddWithValue("@preview", (object?)preview ?? DBNull.Value);
            upsertConv.ExecuteNonQuery();

            // Insert messages
            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage msg = messages[i];
                SerializedMessage serialized = MessageSerializer.Serialize(msg);

                InsertMessage(conn, tx, id, i, msg.Id, serialized);

                // Store extracted attachments
                foreach (ExtractedAttachment att in serialized.Attachments)
                {
                    string storagePath = _attachments.SaveAttachment(id, att.Id, att.Data, att.Extension);
                    InsertAttachment(conn, tx, id, msg.Id.ToString(), att, storagePath);
                }
            }

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Append a single message to an existing conversation.
    /// Used for crash-resilient incremental persistence.
    /// </summary>
    public void AppendMessage(string conversationId, ChatMessage message, int sequence)
    {
        SqliteConnection conn = _db.GetConnection();
        SerializedMessage serialized = MessageSerializer.Serialize(message);

        InsertMessage(conn, null, conversationId, sequence, message.Id, serialized);

        foreach (ExtractedAttachment att in serialized.Attachments)
        {
            string storagePath = _attachments.SaveAttachment(conversationId, att.Id, att.Data, att.Extension);
            InsertAttachment(conn, null, conversationId, message.Id.ToString(), att, storagePath);
        }

        // Update conversation metadata
        using SqliteCommand updateMeta = conn.CreateCommand();
        updateMeta.CommandText = """
            UPDATE conversations SET
                updated_at = @updated_at,
                message_count = (SELECT COUNT(*) FROM messages WHERE conversation_id = @id)
            WHERE id = @id
            """;
        updateMeta.Parameters.AddWithValue("@id", conversationId);
        updateMeta.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.ToString("O"));
        updateMeta.ExecuteNonQuery();
    }

    // ───────────────────────────────────────────────
    // Load
    // ───────────────────────────────────────────────

    /// <summary>
    /// Load visible messages (current context view) without resolving attachment binary data.
    /// Suitable for lightweight UI display where images are lazy-loaded.
    /// </summary>
    public List<ChatMessage>? Load(string id)
    {
        return LoadMessages(id, visibleOnly: true, resolveAttachments: false);
    }

    /// <summary>
    /// Load ALL messages in a conversation (including summarized-away ones) without resolving attachments.
    /// For full history UI display with pagination.
    /// </summary>
    public List<ChatMessage>? LoadFull(string id)
    {
        return LoadMessages(id, visibleOnly: false, resolveAttachments: false);
    }

    /// <summary>
    /// Load visible messages with all attachment binary data resolved inline.
    /// Used for building the LLM context where images must be inline base64.
    /// </summary>
    public List<ChatMessage>? LoadWithAttachments(string id)
    {
        return LoadMessages(id, visibleOnly: true, resolveAttachments: true);
    }

    /// <summary>
    /// Load a single attachment's raw bytes and MIME type for lazy loading in the UI.
    /// </summary>
    public (byte[] data, string mimeType)? LoadAttachment(string attachmentId)
    {
        AttachmentMetadata? meta = GetAttachmentMetadata(attachmentId);
        if (meta is null) return null;
        return MessageSerializer.ResolveAttachment(_attachments, meta);
    }

    // ───────────────────────────────────────────────
    // List / Delete
    // ───────────────────────────────────────────────

    /// <summary>
    /// Id of the most recently updated conversation, or null when the store is empty.
    /// Used by --continue / auto-resume.
    /// </summary>
    public string? GetMostRecentConversationId()
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM conversations ORDER BY updated_at DESC LIMIT 1";
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// List all saved conversations, ordered by most recently updated.
    /// </summary>
    public List<ConversationMetadata> List()
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, label, created_at, updated_at, model, message_count, first_preview, active_skills
            FROM conversations
            ORDER BY updated_at DESC
            """;

        List<ConversationMetadata> result = [];
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string? skillsJson = reader.IsDBNull(7) ? null : reader.GetString(7);
            result.Add(new ConversationMetadata
            {
                Id = reader.GetString(0),
                Label = reader.IsDBNull(1) ? null : reader.GetString(1),
                CreatedAt = DateTime.Parse(reader.GetString(2)),
                UpdatedAt = DateTime.Parse(reader.GetString(3)),
                Model = reader.IsDBNull(4) ? null : reader.GetString(4),
                MessageCount = reader.GetInt32(5),
                FirstMessagePreview = reader.IsDBNull(6) ? null : reader.GetString(6),
                ActiveSkills = !string.IsNullOrEmpty(skillsJson)
                    ? JsonSerializer.Deserialize<List<string>>(skillsJson) ?? []
                    : [],
            });
        }

        return result;
    }

    /// <summary>
    /// Delete a conversation and all associated data (messages, attachments, summaries, snapshots).
    /// </summary>
    public bool Delete(string id)
    {
        _attachments.DeleteConversationAttachments(id);

        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM conversations WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ───────────────────────────────────────────────
    // Summarization support
    // ───────────────────────────────────────────────

    /// <summary>
    /// Save a summary produced by the MessageSummarizer.
    /// </summary>
    public long SaveSummary(string conversationId, string summaryText, int coversThrough, int tokenEstimate)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO summaries (conversation_id, summary_text, created_at, covers_through, token_estimate)
            VALUES (@conv, @text, @at, @through, @tokens);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.Parameters.AddWithValue("@text", summaryText);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@through", coversThrough);
        cmd.Parameters.AddWithValue("@tokens", tokenEstimate);
        return (long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// Mark messages up to a given sequence as compressed and hidden from the visible context.
    /// </summary>
    public void MarkMessagesCompressed(string conversationId, int upToSequence, MessageCompressionState state = MessageCompressionState.Compressed)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE messages
            SET compression_state = @state, visible = 0
            WHERE conversation_id = @conv AND sequence <= @seq AND compression_state < @state
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.Parameters.AddWithValue("@seq", upToSequence);
        cmd.Parameters.AddWithValue("@state", (int)state);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Get the most recent summary for a conversation.
    /// </summary>
    public (long id, string text, int coversThrough)? GetLatestSummary(string conversationId)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, summary_text, covers_through
            FROM summaries
            WHERE conversation_id = @conv
            ORDER BY id DESC LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);
        using SqliteDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2));
    }

    // ───────────────────────────────────────────────
    // Snapshots (restore points)
    // ───────────────────────────────────────────────

    /// <summary>
    /// Create a restore point capturing the current visible message set and active summary.
    /// </summary>
    public long CreateSnapshot(string conversationId, string? label = null)
    {
        SqliteConnection conn = _db.GetConnection();

        // Collect current visible message IDs
        List<string> visibleIds = [];
        using (SqliteCommand getVisible = conn.CreateCommand())
        {
            getVisible.CommandText = "SELECT id FROM messages WHERE conversation_id = @conv AND visible = 1 ORDER BY sequence";
            getVisible.Parameters.AddWithValue("@conv", conversationId);
            using SqliteDataReader reader = getVisible.ExecuteReader();
            while (reader.Read())
                visibleIds.Add(reader.GetString(0));
        }

        // Get latest summary ID
        long? summaryId = null;
        var latestSummary = GetLatestSummary(conversationId);
        if (latestSummary.HasValue)
            summaryId = latestSummary.Value.id;

        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO snapshots (conversation_id, created_at, label, message_count, visible_ids, summary_id)
            VALUES (@conv, @at, @label, @count, @ids, @summary);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@label", (object?)label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@count", visibleIds.Count);
        cmd.Parameters.AddWithValue("@ids", JsonSerializer.Serialize(visibleIds));
        cmd.Parameters.AddWithValue("@summary", summaryId.HasValue ? summaryId.Value : DBNull.Value);
        return (long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// List all snapshots for a conversation.
    /// </summary>
    public List<SnapshotMetadata> ListSnapshots(string conversationId)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, created_at, label, message_count
            FROM snapshots
            WHERE conversation_id = @conv
            ORDER BY id DESC
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);

        List<SnapshotMetadata> result = [];
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new SnapshotMetadata
            {
                Id = reader.GetInt64(0),
                CreatedAt = DateTime.Parse(reader.GetString(1)),
                Label = reader.IsDBNull(2) ? null : reader.GetString(2),
                MessageCount = reader.GetInt32(3),
            });
        }
        return result;
    }

    /// <summary>
    /// Restore a snapshot: reset which messages are visible to match the snapshot's state.
    /// </summary>
    public bool RestoreSnapshot(string conversationId, long snapshotId)
    {
        SqliteConnection conn = _db.GetConnection();

        // Get snapshot data
        string? visibleIdsJson;
        using (SqliteCommand getSnap = conn.CreateCommand())
        {
            getSnap.CommandText = "SELECT visible_ids FROM snapshots WHERE id = @id AND conversation_id = @conv";
            getSnap.Parameters.AddWithValue("@id", snapshotId);
            getSnap.Parameters.AddWithValue("@conv", conversationId);
            visibleIdsJson = getSnap.ExecuteScalar() as string;
        }

        if (string.IsNullOrEmpty(visibleIdsJson)) return false;

        List<string>? visibleIds = JsonSerializer.Deserialize<List<string>>(visibleIdsJson);
        if (visibleIds is null) return false;

        using SqliteTransaction tx = conn.BeginTransaction();
        try
        {
            // Mark all messages as not visible
            using (SqliteCommand hideAll = conn.CreateCommand())
            {
                hideAll.Transaction = tx;
                hideAll.CommandText = "UPDATE messages SET visible = 0 WHERE conversation_id = @conv";
                hideAll.Parameters.AddWithValue("@conv", conversationId);
                hideAll.ExecuteNonQuery();
            }

            // Restore visible flags for snapshot messages
            foreach (string msgId in visibleIds)
            {
                using SqliteCommand restore = conn.CreateCommand();
                restore.Transaction = tx;
                restore.CommandText = "UPDATE messages SET visible = 1 WHERE id = @id AND conversation_id = @conv";
                restore.Parameters.AddWithValue("@id", msgId);
                restore.Parameters.AddWithValue("@conv", conversationId);
                restore.ExecuteNonQuery();
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ───────────────────────────────────────────────
    // Ensure conversation exists (for incremental persistence)
    // ───────────────────────────────────────────────

    /// <summary>
    /// Ensure a conversation row exists. Used by ConversationMemoryManager for incremental appends.
    /// </summary>
    public void EnsureConversation(string id, string? model = null)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO conversations (id, created_at, updated_at, model, message_count)
            VALUES (@id, @at, @at, @model, 0)
            """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@model", (object?)model ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Get the current message count for a conversation.
    /// </summary>
    public int GetMessageCount(string conversationId)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM messages WHERE conversation_id = @conv";
        cmd.Parameters.AddWithValue("@conv", conversationId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ───────────────────────────────────────────────
    // Private helpers
    // ───────────────────────────────────────────────

    private List<ChatMessage>? LoadMessages(string id, bool visibleOnly, bool resolveAttachments)
    {
        SqliteConnection conn = _db.GetConnection();

        // Check conversation exists
        using (SqliteCommand check = conn.CreateCommand())
        {
            check.CommandText = "SELECT 1 FROM conversations WHERE id = @id";
            check.Parameters.AddWithValue("@id", id);
            if (check.ExecuteScalar() is null) return null;
        }

        // Load messages
        string visibleFilter = visibleOnly ? "AND visible = 1" : "";
        using SqliteCommand msgCmd = conn.CreateCommand();
        msgCmd.CommandText = $"""
            SELECT id, role, content, parts_json, sequence, compression_state, visible
            FROM messages
            WHERE conversation_id = @conv {visibleFilter}
            ORDER BY sequence
            """;
        msgCmd.Parameters.AddWithValue("@conv", id);

        // If resolving attachments, pre-load attachment metadata for this conversation
        Dictionary<string, AttachmentMetadata>? attachmentMap = null;
        if (resolveAttachments)
        {
            attachmentMap = LoadAttachmentMap(conn, id);
        }

        List<ChatMessage> result = [];
        using SqliteDataReader reader = msgCmd.ExecuteReader();
        while (reader.Read())
        {
            string msgIdStr = reader.GetString(0);
            Guid msgId = Guid.TryParse(msgIdStr, out Guid parsed) ? parsed : Guid.NewGuid();
            string role = reader.GetString(1);
            string? content = reader.IsDBNull(2) ? null : reader.GetString(2);
            string? partsJson = reader.IsDBNull(3) ? null : reader.GetString(3);

            ChatMessage msg = resolveAttachments && attachmentMap is not null
                ? MessageSerializer.DeserializeWithAttachments(role, content, partsJson, msgId, _attachments, attachmentMap)
                : MessageSerializer.DeserializeLightweight(role, content, partsJson, msgId);

            result.Add(msg);
        }

        return result;
    }

    private Dictionary<string, AttachmentMetadata> LoadAttachmentMap(SqliteConnection conn, string conversationId)
    {
        Dictionary<string, AttachmentMetadata> map = new(StringComparer.OrdinalIgnoreCase);

        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, message_id, conversation_id, file_name, mime_type, media_type, size_bytes, storage_path, created_at
            FROM attachments
            WHERE conversation_id = @conv
            """;
        cmd.Parameters.AddWithValue("@conv", conversationId);

        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string attId = reader.GetString(0);
            map[attId] = new AttachmentMetadata
            {
                Id = attId,
                MessageId = reader.GetString(1),
                ConversationId = reader.GetString(2),
                FileName = reader.IsDBNull(3) ? null : reader.GetString(3),
                MimeType = reader.GetString(4),
                MediaType = (AttachmentMediaType)reader.GetInt32(5),
                SizeBytes = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                StoragePath = reader.GetString(7),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
            };
        }

        return map;
    }

    private AttachmentMetadata? GetAttachmentMetadata(string attachmentId)
    {
        SqliteConnection conn = _db.GetConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, message_id, conversation_id, file_name, mime_type, media_type, size_bytes, storage_path, created_at
            FROM attachments
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", attachmentId);

        using SqliteDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new AttachmentMetadata
        {
            Id = reader.GetString(0),
            MessageId = reader.GetString(1),
            ConversationId = reader.GetString(2),
            FileName = reader.IsDBNull(3) ? null : reader.GetString(3),
            MimeType = reader.GetString(4),
            MediaType = (AttachmentMediaType)reader.GetInt32(5),
            SizeBytes = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
            StoragePath = reader.GetString(7),
            CreatedAt = DateTime.Parse(reader.GetString(8)),
        };
    }

    private static void InsertMessage(
        SqliteConnection conn, SqliteTransaction? tx, string conversationId, int sequence,
        Guid messageId, SerializedMessage serialized)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT OR REPLACE INTO messages (id, conversation_id, sequence, role, content, parts_json, created_at, token_estimate, compression_state, visible)
            VALUES (@id, @conv, @seq, @role, @content, @parts, @at, @tokens, 0, 1)
            """;
        cmd.Parameters.AddWithValue("@id", messageId.ToString());
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.Parameters.AddWithValue("@seq", sequence);
        cmd.Parameters.AddWithValue("@role", serialized.Role);
        cmd.Parameters.AddWithValue("@content", (object?)serialized.Content ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@parts", (object?)serialized.PartsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@tokens", serialized.TokenEstimate);
        cmd.ExecuteNonQuery();
    }

    private static void InsertAttachment(
        SqliteConnection conn, SqliteTransaction? tx, string conversationId, string messageId,
        ExtractedAttachment att, string storagePath)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT OR REPLACE INTO attachments (id, message_id, conversation_id, file_name, mime_type, media_type, size_bytes, storage_path, created_at)
            VALUES (@id, @msg, @conv, @name, @mime, @media, @size, @path, @at)
            """;
        cmd.Parameters.AddWithValue("@id", att.Id);
        cmd.Parameters.AddWithValue("@msg", messageId);
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.Parameters.AddWithValue("@name", (object?)att.FileName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@mime", att.MimeType);
        cmd.Parameters.AddWithValue("@media", (int)att.MediaType);
        cmd.Parameters.AddWithValue("@size", att.Data.Length);
        cmd.Parameters.AddWithValue("@path", storagePath);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void HideMessages(SqliteConnection conn, SqliteTransaction tx, string conversationId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE messages SET visible = 0 WHERE conversation_id = @conv";
        cmd.Parameters.AddWithValue("@conv", conversationId);
        cmd.ExecuteNonQuery();
    }

    private static string Slugify(string text)
    {
        char[] chars = text.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .Select(c => c == ' ' ? '-' : c)
            .ToArray();
        return string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

/// <summary>
/// Metadata for a conversation snapshot (restore point).
/// </summary>
public sealed class SnapshotMetadata
{
    public long Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Label { get; init; }
    public int MessageCount { get; init; }
}
