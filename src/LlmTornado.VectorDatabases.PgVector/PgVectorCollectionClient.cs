using Npgsql;
using System.Text.Json;
using System.Text;

namespace LlmTornado.VectorDatabases.PgVector;

public class PgVectorCollectionClient
{
    private readonly PgVectorCollection _collection;
    private readonly PgVectorClient _client;

    public PgVectorCollectionClient(PgVectorCollection collection, PgVectorClient client)
    {
        _collection = collection;
        _client = client;
    }

    public PgVectorCollection Collection => _collection;

    private string GetTableName() => $"{_collection.Name}_vectors";

    public async Task<PgVectorEntry?> GetAsync(string id)
    {
        var entries = await GetAsync(new[] { id });
        return entries.FirstOrDefault();
    }

    public async Task<List<PgVectorEntry>> GetAsync(string[] ids)
    {
        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();
        var query = $@"
            SELECT id, document, metadata
            FROM {schema}.{tableName} 
            WHERE id = ANY(@ids)";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("ids", ids);

        var entries = new List<PgVectorEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var document = reader.IsDBNull(1) ? null : reader.GetString(1);
            var metadataJson = reader.IsDBNull(2) ? null : reader.GetString(2);
            var metadata = string.IsNullOrEmpty(metadataJson) 
                ? null 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson!);
            
            //float[]? embedding = null;
            //if (!reader.IsDBNull(3))
            //{
            //    var embeddingStr = reader.GetString(3);
            //    embedding = ParseVector(embeddingStr);
            //}

            entries.Add(new PgVectorEntry(id, document, metadata));
        }

        return entries;
    }

    public async Task<List<PgVectorEntry>> QueryAsync(float[] queryEmbedding, int topK = 10, Dictionary<string, object>? whereMetadata = null)
    {
        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();
        
        var queryBuilder = new StringBuilder();
        queryBuilder.Append($@"
            SELECT id, document, metadata, embedding, 
                   (embedding <=> @embedding::vector) as distance 
            FROM {schema}.{tableName}");

        if (whereMetadata != null && whereMetadata.Count > 0)
        {
            queryBuilder.Append(" WHERE ");
            queryBuilder.Append(BuildMetadataFilter(whereMetadata));
        }

        queryBuilder.Append($@"
            ORDER BY embedding <=> @embedding::vector 
            LIMIT @limit");

        using var cmd = new NpgsqlCommand(queryBuilder.ToString(), connection);
        cmd.Parameters.AddWithValue("embedding", $"[{string.Join(",", queryEmbedding)}]");
        cmd.Parameters.AddWithValue("limit", topK);

        var entries = new List<PgVectorEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var document = reader.IsDBNull(1) ? null : reader.GetString(1);
            var metadataJson = reader.IsDBNull(2) ? null : reader.GetString(2);
            var metadata = string.IsNullOrEmpty(metadataJson) 
                ? null 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson!);

            float[]? embedding = null;
            //if (!reader.IsDBNull(3))
            //{
            //    var embeddingStr = reader.GetString(3);
            //    embedding = ParseVector(embeddingStr);
            //}

            var distance = reader.GetFloat(4);

            entries.Add(new PgVectorEntry(id, document, metadata, embedding, distance));
        }

        return entries;
    }

    public async Task AddAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        if (ids.Count == 0) return;

        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();

        using var transaction = connection.BeginTransaction();
        try
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var query = $@"
                    INSERT INTO {schema}.{tableName} (id, document, metadata, embedding)
                    VALUES (@id, @document, @metadata::jsonb, @embedding::vector)";

                using var cmd = new NpgsqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("id", ids[i]);
                cmd.Parameters.AddWithValue("document", documents != null && i < documents.Count ? (object)documents[i] : DBNull.Value);
                
                var metadataJson = metadatas != null && i < metadatas.Count 
                    ? JsonSerializer.Serialize(metadatas[i]) 
                    : "{}";
                cmd.Parameters.AddWithValue("metadata", metadataJson);
                
                var embeddingStr = embeddings != null && i < embeddings.Count 
                    ? $"[{string.Join(",", embeddings[i])}]" 
                    : null;
                cmd.Parameters.AddWithValue("embedding", embeddingStr != null ? (object)embeddingStr : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        if (ids.Count == 0) return;

        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();

        using var transaction = connection.BeginTransaction();
        try
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var setParts = new List<string>();
                var cmd = new NpgsqlCommand { Connection = connection, Transaction = transaction };

                if (documents != null && i < documents.Count)
                {
                    setParts.Add("document = @document");
                    cmd.Parameters.AddWithValue("document", documents[i]);
                }

                if (metadatas != null && i < metadatas.Count)
                {
                    setParts.Add("metadata = @metadata::jsonb");
                    cmd.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(metadatas[i]));
                }

                if (embeddings != null && i < embeddings.Count)
                {
                    setParts.Add("embedding = @embedding::vector");
                    cmd.Parameters.AddWithValue("embedding", $"[{string.Join(",", embeddings[i])}]");
                }

                if (setParts.Count > 0)
                {
                    var query = $@"
                        UPDATE {schema}.{tableName} 
                        SET {string.Join(", ", setParts)}
                        WHERE id = @id";

                    cmd.CommandText = query;
                    cmd.Parameters.AddWithValue("id", ids[i]);

                    await cmd.ExecuteNonQueryAsync();
                }

                cmd.Dispose();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpsertAsync(List<string> ids, List<float[]>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
    {
        if (ids.Count == 0) return;

        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();

        using var transaction = connection.BeginTransaction();
        try
        {
            for (int i = 0; i < ids.Count; i++)
            {
                var query = $@"
                    INSERT INTO {schema}.{tableName} (id, document, metadata, embedding)
                    VALUES (@id, @document, @metadata::jsonb, @embedding::vector)
                    ON CONFLICT (id) DO UPDATE SET
                        document = EXCLUDED.document,
                        metadata = EXCLUDED.metadata,
                        embedding = EXCLUDED.embedding";

                using var cmd = new NpgsqlCommand(query, connection, transaction);
                cmd.Parameters.AddWithValue("id", ids[i]);
                cmd.Parameters.AddWithValue("document", documents != null && i < documents.Count ? (object)documents[i] : DBNull.Value);
                
                var metadataJson = metadatas != null && i < metadatas.Count 
                    ? JsonSerializer.Serialize(metadatas[i]) 
                    : "{}";
                cmd.Parameters.AddWithValue("metadata", metadataJson);
                
                var embeddingStr = embeddings != null && i < embeddings.Count 
                    ? $"[{string.Join(",", embeddings[i])}]" 
                    : null;
                cmd.Parameters.AddWithValue("embedding", embeddingStr != null ? (object)embeddingStr : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAsync(List<string> ids)
    {
        if (ids.Count == 0) return;

        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();
        var query = $"DELETE FROM {schema}.{tableName} WHERE id = ANY(@ids)";

        using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAllAsync()
    {
        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();
        var query = $"DELETE FROM {schema}.{tableName}";

        using var cmd = new NpgsqlCommand(query, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> CountAsync()
    {
        using var connection = _client.CreateConnection();
        await connection.OpenAsync();

        var schema = _client.GetSchema();
        var tableName = GetTableName();
        var query = $"SELECT COUNT(*) FROM {schema}.{tableName}";

        using var cmd = new NpgsqlCommand(query, connection);
        var result = await cmd.ExecuteScalarAsync();
        
        return Convert.ToInt32(result);
    }

    private static float[] ParseVector(string vectorStr)
    {
        // Vector format: [1.0,2.0,3.0]
        var cleaned = vectorStr.Trim('[', ']');
        var parts = cleaned.Split(',');
        return parts.Select(p => float.Parse(p.Trim())).ToArray();
    }

    private static string BuildMetadataFilter(Dictionary<string, object> whereMetadata)
    {
        var conditions = new List<string>();
        
        foreach (var kvp in whereMetadata)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (value is Dictionary<string, object> operatorDict)
            {
                foreach (var op in operatorDict)
                {
                    var condition = BuildCondition(key, op.Key, op.Value);
                    if (condition != null)
                    {
                        conditions.Add(condition);
                    }
                }
            }
            else
            {
                // Direct equality
                conditions.Add($"metadata->'{key}' = '\"{value}\"'");
            }
        }

        return string.Join(" AND ", conditions);
    }

    private static string? BuildCondition(string key, string op, object value)
    {
        return op switch
        {
            "$eq" => $"metadata->'{key}' = '{JsonSerializer.Serialize(value)}'",
            "$ne" => $"metadata->'{key}' != '{JsonSerializer.Serialize(value)}'",
            "$gt" => $"(metadata->'{key}')::numeric > {value}",
            "$gte" => $"(metadata->'{key}')::numeric >= {value}",
            "$lt" => $"(metadata->'{key}')::numeric < {value}",
            "$lte" => $"(metadata->'{key}')::numeric <= {value}",
            "$in" => value is Array arr 
                ? $"metadata->'{key}' IN ({string.Join(",", arr.Cast<object>().Select(v => $"'{JsonSerializer.Serialize(v)}'"))})" 
                : null,
            "$nin" => value is Array arr2 
                ? $"metadata->'{key}' NOT IN ({string.Join(",", arr2.Cast<object>().Select(v => $"'{JsonSerializer.Serialize(v)}'"))})" 
                : null,
            _ => null
        };
    }
}
