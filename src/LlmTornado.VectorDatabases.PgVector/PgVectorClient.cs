using Npgsql;
using System.Text.Json;

namespace LlmTornado.VectorDatabases.PgVector;

public class PgVectorClient
{
    private readonly string _connectionString;
    private readonly string _schema;

    public PgVectorClient(PgVectorConfigurationOptions options)
    {
        _connectionString = options.ConnectionString;
        _schema = options.Schema ?? "public";
    }

    public async Task InitializeAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Enable vector extension
        using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<PgVectorCollection>> ListCollectionsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collections = new List<PgVectorCollection>();
        var query = $@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = @schema 
            AND table_name LIKE '%_vectors'";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("schema", _schema);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString(0);
            var collectionName = tableName.Replace("_vectors", "");
            collections.Add(new PgVectorCollection(collectionName, 0));
        }

        return collections;
    }

    public async Task<PgVectorCollection?> GetCollectionAsync(string name)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tableName = $"{name}_vectors";
        var query = $@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = @schema 
            AND table_name = @tableName";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("schema", _schema);
        cmd.Parameters.AddWithValue("tableName", tableName);

        var exists = await cmd.ExecuteScalarAsync();
        if (exists == null)
        {
            return null;
        }

        return new PgVectorCollection(name, 0);
    }

    public async Task<PgVectorCollection> CreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tableName = $"{name}_vectors";
        var createTableQuery = $@"
            CREATE TABLE IF NOT EXISTS {_schema}.{tableName} (
                id TEXT PRIMARY KEY,
                document TEXT,
                metadata JSONB,
                embedding vector({vectorDimension})
            )";

        using var cmd = new NpgsqlCommand(createTableQuery, connection);
        await cmd.ExecuteNonQueryAsync();

        // Create index for vector similarity search
        var createIndexQuery = $@"
            CREATE INDEX IF NOT EXISTS {tableName}_embedding_idx 
            ON {_schema}.{tableName} 
            USING ivfflat (embedding vector_cosine_ops)";

        using var indexCmd = new NpgsqlCommand(createIndexQuery, connection);
        await indexCmd.ExecuteNonQueryAsync();

        // Create GIN index for metadata search
        var createMetadataIndexQuery = $@"
            CREATE INDEX IF NOT EXISTS {tableName}_metadata_idx 
            ON {_schema}.{tableName} 
            USING gin (metadata)";

        using var metadataIndexCmd = new NpgsqlCommand(createMetadataIndexQuery, connection);
        await metadataIndexCmd.ExecuteNonQueryAsync();

        return new PgVectorCollection(name, vectorDimension, metadata);
    }

    public async Task<PgVectorCollection> GetOrCreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        var collection = await GetCollectionAsync(name);
        if (collection != null)
        {
            return collection;
        }

        return await CreateCollectionAsync(name, vectorDimension, metadata);
    }

    public async Task DeleteCollectionAsync(string name)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tableName = $"{name}_vectors";
        var query = $"DROP TABLE IF EXISTS {_schema}.{tableName}";

        using var cmd = new NpgsqlCommand(query, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    internal NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    internal string GetSchema() => _schema;
}
