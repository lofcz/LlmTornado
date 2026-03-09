using Microsoft.Data.Sqlite;

namespace LlmTornado.Cli.Core.Storage;

/// <summary>
/// Manages the SQLite database lifecycle for conversation persistence.
/// Handles schema creation and connection management.
/// </summary>
public sealed class ConversationDatabase : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    /// <summary>
    /// The directory containing the database file. Attachment storage lives alongside this.
    /// </summary>
    public string DatabaseDirectory { get; }

    public ConversationDatabase(string databasePath)
    {
        DatabaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath))
                            ?? throw new ArgumentException("Invalid database path", nameof(databasePath));
        Directory.CreateDirectory(DatabaseDirectory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        EnsureSchema();
    }

    /// <summary>
    /// Get an open connection to the database. The connection is reused across calls.
    /// </summary>
    public SqliteConnection GetConnection()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        // Enable WAL for concurrent reads during streaming
        using SqliteCommand walCmd = _connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        return _connection;
    }

    /// <summary>
    /// Create a new independent connection (e.g. for concurrent reads).
    /// Caller is responsible for disposing it.
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        SqliteConnection conn = new(_connectionString);
        conn.Open();
        return conn;
    }

    private void EnsureSchema()
    {
        using SqliteConnection conn = CreateConnection();

        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS conversations (
                id              TEXT PRIMARY KEY,
                label           TEXT,
                created_at      TEXT NOT NULL,
                updated_at      TEXT NOT NULL,
                model           TEXT,
                active_agent    TEXT,
                active_skills   TEXT,
                message_count   INTEGER NOT NULL DEFAULT 0,
                first_preview   TEXT
            );

            CREATE TABLE IF NOT EXISTS messages (
                id                  TEXT PRIMARY KEY,
                conversation_id     TEXT NOT NULL,
                sequence            INTEGER NOT NULL,
                role                TEXT NOT NULL,
                content             TEXT,
                parts_json          TEXT,
                created_at          TEXT NOT NULL,
                token_estimate      INTEGER,
                compression_state   INTEGER NOT NULL DEFAULT 0,
                visible             INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_messages_conversation_seq
                ON messages(conversation_id, sequence);

            CREATE TABLE IF NOT EXISTS attachments (
                id                  TEXT PRIMARY KEY,
                message_id          TEXT NOT NULL,
                conversation_id     TEXT NOT NULL,
                file_name           TEXT,
                mime_type           TEXT NOT NULL,
                media_type          INTEGER NOT NULL,
                size_bytes          INTEGER,
                storage_path        TEXT NOT NULL,
                created_at          TEXT NOT NULL,
                FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_attachments_message
                ON attachments(message_id);

            CREATE INDEX IF NOT EXISTS idx_attachments_conversation
                ON attachments(conversation_id);

            CREATE TABLE IF NOT EXISTS summaries (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                conversation_id     TEXT NOT NULL,
                summary_text        TEXT NOT NULL,
                created_at          TEXT NOT NULL,
                covers_through      INTEGER,
                token_estimate      INTEGER,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_summaries_conversation
                ON summaries(conversation_id);

            CREATE TABLE IF NOT EXISTS snapshots (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                conversation_id     TEXT NOT NULL,
                created_at          TEXT NOT NULL,
                label               TEXT,
                message_count       INTEGER,
                visible_ids         TEXT,
                summary_id          INTEGER,
                metadata_json       TEXT,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
                FOREIGN KEY (summary_id) REFERENCES summaries(id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS idx_snapshots_conversation
                ON snapshots(conversation_id);

            PRAGMA foreign_keys = ON;
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
