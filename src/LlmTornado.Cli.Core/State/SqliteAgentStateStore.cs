using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LlmTornado.Cli.Core.State;

public sealed class SqliteAgentStateStore : IAgentStateStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SqliteAgentStateStore(string databasePath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        EnsureSchema();
    }

    public AgentMemoryRecord StoreMemory(string? key, string content, IReadOnlyList<string> tags, string? sourceConversationId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Memory content is required.", nameof(content));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_memories (memory_key, content, tags_json, created_at, updated_at, source_conversation_id)
            VALUES (@key, @content, @tags, @created, @updated, @source);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@key", DbValue(Normalize(key)));
        cmd.Parameters.AddWithValue("@content", content.Trim());
        cmd.Parameters.AddWithValue("@tags", SerializeTags(tags));
        cmd.Parameters.AddWithValue("@created", now.ToString("O"));
        cmd.Parameters.AddWithValue("@updated", now.ToString("O"));
        cmd.Parameters.AddWithValue("@source", DbValue(Normalize(sourceConversationId)));

        long id = (long)cmd.ExecuteScalar()!;
        return GetMemory(id)!;
    }

    public IReadOnlyList<AgentMemoryRecord> SearchMemories(string? query, string? tag, int limit)
    {
        int cappedLimit = NormalizeLimit(limit);
        string? normalizedQuery = Normalize(query);

        using SqliteCommand cmd = Connection.CreateCommand();
        if (normalizedQuery is null)
        {
            cmd.CommandText = """
                SELECT id, memory_key, content, tags_json, created_at, updated_at, source_conversation_id
                FROM agent_memories
                ORDER BY updated_at DESC, id DESC
                LIMIT @limit
                """;
        }
        else
        {
            cmd.CommandText = """
                SELECT id, memory_key, content, tags_json, created_at, updated_at, source_conversation_id
                FROM agent_memories
                WHERE content LIKE @query OR memory_key LIKE @query
                ORDER BY updated_at DESC, id DESC
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@query", $"%{normalizedQuery}%");
        }

        cmd.Parameters.AddWithValue("@limit", cappedLimit * 4);
        List<AgentMemoryRecord> records = ReadMemories(cmd);
        string? normalizedTag = Normalize(tag);
        if (normalizedTag is not null)
        {
            records = records
                .Where(record => record.Tags.Any(t => t.Equals(normalizedTag, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return records.Take(cappedLimit).ToList();
    }

    public AgentMemoryRecord? GetMemory(long id)
    {
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, memory_key, content, tags_json, created_at, updated_at, source_conversation_id
            FROM agent_memories
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);
        return ReadMemories(cmd).FirstOrDefault();
    }

    public bool DeleteMemory(long id)
    {
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM agent_memories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public AgentStateRecord SetState(string key, string value, string? contentType)
    {
        string normalizedKey = Normalize(key) ?? throw new ArgumentException("State key is required.", nameof(key));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_state (state_key, value, content_type, created_at, updated_at)
            VALUES (@key, @value, @type, @created, @updated)
            ON CONFLICT(state_key) DO UPDATE SET
                value = @value,
                content_type = @type,
                updated_at = @updated
            """;
        cmd.Parameters.AddWithValue("@key", normalizedKey);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.Parameters.AddWithValue("@type", DbValue(Normalize(contentType)));
        cmd.Parameters.AddWithValue("@created", now.ToString("O"));
        cmd.Parameters.AddWithValue("@updated", now.ToString("O"));
        cmd.ExecuteNonQuery();

        return GetState(normalizedKey)!;
    }

    public AgentStateRecord? GetState(string key)
    {
        string? normalizedKey = Normalize(key);
        if (normalizedKey is null)
            return null;

        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            SELECT state_key, value, content_type, created_at, updated_at
            FROM agent_state
            WHERE state_key = @key
            """;
        cmd.Parameters.AddWithValue("@key", normalizedKey);
        return ReadState(cmd).FirstOrDefault();
    }

    public IReadOnlyList<AgentStateRecord> ListState(string? prefix, int limit)
    {
        int cappedLimit = NormalizeLimit(limit);
        string? normalizedPrefix = Normalize(prefix);
        using SqliteCommand cmd = Connection.CreateCommand();
        if (normalizedPrefix is null)
        {
            cmd.CommandText = """
                SELECT state_key, value, content_type, created_at, updated_at
                FROM agent_state
                ORDER BY state_key
                LIMIT @limit
                """;
        }
        else
        {
            cmd.CommandText = """
                SELECT state_key, value, content_type, created_at, updated_at
                FROM agent_state
                WHERE state_key LIKE @prefix
                ORDER BY state_key
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@prefix", $"{normalizedPrefix}%");
        }

        cmd.Parameters.AddWithValue("@limit", cappedLimit);
        return ReadState(cmd);
    }

    public bool DeleteState(string key)
    {
        string? normalizedKey = Normalize(key);
        if (normalizedKey is null)
            return false;

        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM agent_state WHERE state_key = @key";
        cmd.Parameters.AddWithValue("@key", normalizedKey);
        return cmd.ExecuteNonQuery() > 0;
    }

    public AgentStateSnapshotRecord CreateSnapshot(string? label)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<AgentStateRecord> state = ListAllState();
        string stateJson = JsonSerializer.Serialize(state, JsonOptions);

        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_state_snapshots (label, created_at, state_json)
            VALUES (@label, @created, @state);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@label", DbValue(Normalize(label)));
        cmd.Parameters.AddWithValue("@created", now.ToString("O"));
        cmd.Parameters.AddWithValue("@state", stateJson);
        long id = (long)cmd.ExecuteScalar()!;

        return GetSnapshot(id)!;
    }

    public IReadOnlyList<AgentStateSnapshotRecord> ListSnapshots(int limit)
    {
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, label, created_at, state_json
            FROM agent_state_snapshots
            ORDER BY id DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", NormalizeLimit(limit));
        return ReadSnapshots(cmd);
    }

    public AgentStateSnapshotRecord? GetSnapshot(long id)
    {
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, label, created_at, state_json
            FROM agent_state_snapshots
            WHERE id = @id
            """;
        cmd.Parameters.AddWithValue("@id", id);
        return ReadSnapshots(cmd).FirstOrDefault();
    }

    public bool RestoreSnapshot(long id)
    {
        AgentStateSnapshotRecord? snapshot = GetSnapshot(id);
        if (snapshot is null)
            return false;

        using SqliteTransaction tx = Connection.BeginTransaction();
        try
        {
            using (SqliteCommand delete = Connection.CreateCommand())
            {
                delete.Transaction = tx;
                delete.CommandText = "DELETE FROM agent_state";
                delete.ExecuteNonQuery();
            }

            foreach (AgentStateRecord record in snapshot.State)
            {
                using SqliteCommand insert = Connection.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = """
                    INSERT INTO agent_state (state_key, value, content_type, created_at, updated_at)
                    VALUES (@key, @value, @type, @created, @updated)
                    """;
                insert.Parameters.AddWithValue("@key", record.Key);
                insert.Parameters.AddWithValue("@value", record.Value);
                insert.Parameters.AddWithValue("@type", DbValue(record.ContentType));
                insert.Parameters.AddWithValue("@created", record.CreatedAt.ToString("O"));
                insert.Parameters.AddWithValue("@updated", record.UpdatedAt.ToString("O"));
                insert.ExecuteNonQuery();
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

    private SqliteConnection Connection
    {
        get
        {
            if (_connection is not null)
                return _connection;

            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            using SqliteCommand wal = _connection.CreateCommand();
            wal.CommandText = "PRAGMA journal_mode=WAL;";
            wal.ExecuteNonQuery();
            return _connection;
        }
    }

    private void EnsureSchema()
    {
        using SqliteConnection conn = new(_connectionString);
        conn.Open();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS agent_memories (
                id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                memory_key              TEXT,
                content                 TEXT NOT NULL,
                tags_json               TEXT,
                created_at              TEXT NOT NULL,
                updated_at              TEXT NOT NULL,
                source_conversation_id  TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_agent_memories_key
                ON agent_memories(memory_key);

            CREATE INDEX IF NOT EXISTS idx_agent_memories_updated
                ON agent_memories(updated_at);

            CREATE TABLE IF NOT EXISTS agent_state (
                state_key       TEXT PRIMARY KEY,
                value           TEXT NOT NULL,
                content_type    TEXT,
                created_at      TEXT NOT NULL,
                updated_at      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS agent_state_snapshots (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                label       TEXT,
                created_at  TEXT NOT NULL,
                state_json  TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static List<AgentMemoryRecord> ReadMemories(SqliteCommand cmd)
    {
        List<AgentMemoryRecord> result = [];
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new AgentMemoryRecord(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2),
                DeserializeTags(reader.IsDBNull(3) ? null : reader.GetString(3)),
                DateTimeOffset.Parse(reader.GetString(4)),
                DateTimeOffset.Parse(reader.GetString(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }

        return result;
    }

    private static List<AgentStateRecord> ReadState(SqliteCommand cmd)
    {
        List<AgentStateRecord> result = [];
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new AgentStateRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3)),
                DateTimeOffset.Parse(reader.GetString(4))));
        }

        return result;
    }

    private IReadOnlyList<AgentStateRecord> ListAllState()
    {
        using SqliteCommand cmd = Connection.CreateCommand();
        cmd.CommandText = """
            SELECT state_key, value, content_type, created_at, updated_at
            FROM agent_state
            ORDER BY state_key
            """;
        return ReadState(cmd);
    }

    private static List<AgentStateSnapshotRecord> ReadSnapshots(SqliteCommand cmd)
    {
        List<AgentStateSnapshotRecord> result = [];
        using SqliteDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string stateJson = reader.GetString(3);
            List<AgentStateRecord> state =
                JsonSerializer.Deserialize<List<AgentStateRecord>>(stateJson, JsonOptions) ?? [];

            result.Add(new AgentStateSnapshotRecord(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2)),
                state));
        }

        return result;
    }

    private static string? Normalize(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static int NormalizeLimit(int limit) => Math.Clamp(limit <= 0 ? 20 : limit, 1, 200);

    private static string SerializeTags(IReadOnlyList<string> tags)
    {
        List<string> normalized = tags
            .Select(Normalize)
            .Where(tag => tag is not null)
            .Select(tag => tag!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static object DbValue(string? value) => value is null ? DBNull.Value : value;

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
