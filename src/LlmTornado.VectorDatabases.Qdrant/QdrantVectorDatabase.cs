using Qdrant.Client;
using Qdrant.Client.Grpc;
using LlmTornado.VectorDatabases;
using static Qdrant.Client.Grpc.Conditions;
using QdrantRange = Qdrant.Client.Grpc.Range;

namespace LlmTornado.VectorDatabases.Qdrant;

/// <summary>
/// Qdrant vector database implementation of IVectorDatabase.
/// This class provides integration with Qdrant for vector storage and similarity search.
/// </summary>
public class QdrantVectorDatabase : IVectorDatabase
{
    private readonly QdrantClient _client;
    private string _collectionName = "default_collection";
    private readonly int _vectorDimension;

    /// <summary>
    /// Initializes a new instance of the QdrantVectorDatabase class.
    /// </summary>
    /// <param name="host">Qdrant server host (default: localhost)</param>
    /// <param name="port">Qdrant server port (default: 6334 for gRPC)</param>
    /// <param name="vectorDimension">The dimension of vectors to be stored (default: 1536)</param>
    /// <param name="https">Whether to use HTTPS (default: false)</param>
    /// <param name="apiKey">Optional API key for authentication</param>
    public QdrantVectorDatabase(
        string host = "localhost",
        int port = 6334,
        int vectorDimension = 1536,
        bool https = false,
        string? apiKey = null)
    {
        _vectorDimension = vectorDimension;
        _client = new QdrantClient(host, port, https, apiKey);
        
        // Test connection
        Task.Run(async () => await TestConnectionAsync()).Wait();
    }

    /// <summary>
    /// Tests the connection to Qdrant server.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        try
        {
            // Try to get collections to verify connection
            await _client.ListCollectionsAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Qdrant instance not reachable. Please ensure Qdrant is running and accessible.", ex);
        }
    }

    /// <summary>
    /// Initializes or switches to a collection.
    /// </summary>
    /// <param name="collectionName">Name of the collection</param>
    public async Task InitializeCollectionAsync(string collectionName)
    {
        _collectionName = collectionName;
        
        // Check if collection exists
        var collections = await _client.ListCollectionsAsync();
        bool exists = collections.Any(c => c == collectionName);

        if (!exists)
        {
            // Create collection with vector configuration
            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams { Size = (ulong)_vectorDimension, Distance = Distance.Cosine }
            );
        }
    }

    /// <summary>
    /// Deletes a collection from Qdrant.
    /// </summary>
    /// <param name="collectionName">Name of the collection to delete</param>
    public async Task DeleteCollectionAsync(string collectionName)
    {
        await _client.DeleteCollectionAsync(collectionName);
        
        if (_collectionName == collectionName)
        {
            _collectionName = "default_collection";
        }
    }

    private void ThrowIfCollectionNotInitialized()
    {
        if (string.IsNullOrEmpty(_collectionName))
        {
            throw new InvalidOperationException(
                "Collection is not initialized. Please call InitializeCollectionAsync first.");
        }
    }

    /// <summary>
    /// Gets the current collection name.
    /// </summary>
    public string GetCollectionName() => _collectionName;

    /// <summary>
    /// Adds documents to the vector database.
    /// </summary>
    public void AddDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await AddDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Adds documents to the vector database asynchronously.
    /// </summary>
    public async Task AddDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        var points = new List<PointStruct>();
        
        foreach (var doc in documents)
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = doc.Id },
                Vectors = doc.Embedding ?? Array.Empty<float>(),
                Payload = { }
            };

            // Add content to payload
            if (!string.IsNullOrEmpty(doc.Content))
            {
                point.Payload.Add("content", doc.Content);
            }

            // Add metadata to payload
            if (doc.Metadata != null)
            {
                foreach (var kvp in doc.Metadata)
                {
                    point.Payload.Add(kvp.Key, ConvertToValue(kvp.Value));
                }
            }

            points.Add(point);
        }

        await _client.UpsertAsync(_collectionName, points);
    }

    /// <summary>
    /// Retrieves documents by their IDs.
    /// </summary>
    public VectorDocument[]? GetDocuments(string[] ids)
    {
        return Task.Run(async () => await GetDocumentsAsync(ids)).Result;
    }

    /// <summary>
    /// Retrieves documents by their IDs asynchronously.
    /// </summary>
    public async Task<VectorDocument[]> GetDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();

        var pointIds = ids.Select(id => new PointId { Uuid = id }).ToList();
        var points = await _client.RetrieveAsync(
            _collectionName,
            pointIds,
            true, // with payload
            true  // with vectors
        );

        return points.Select(ConvertToVectorDocument).ToArray();
    }

    /// <summary>
    /// Updates existing documents in the vector database.
    /// </summary>
    public void UpdateDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpdateDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Updates existing documents in the vector database asynchronously.
    /// </summary>
    public async Task UpdateDocumentsAsync(VectorDocument[] documents)
    {
        // Qdrant doesn't have a separate update operation - use upsert
        await UpsertDocumentsAsync(documents);
    }

    /// <summary>
    /// Inserts or updates documents in the vector database.
    /// </summary>
    public void UpsertDocuments(VectorDocument[] documents)
    {
        Task.Run(async () => await UpsertDocumentsAsync(documents)).Wait();
    }

    /// <summary>
    /// Inserts or updates documents in the vector database asynchronously.
    /// </summary>
    public async Task UpsertDocumentsAsync(VectorDocument[] documents)
    {
        ThrowIfCollectionNotInitialized();

        var points = new List<PointStruct>();

        foreach (var doc in documents)
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = doc.Id },
                Vectors = doc.Embedding ?? Array.Empty<float>(),
                Payload = { }
            };

            // Add content to payload
            if (!string.IsNullOrEmpty(doc.Content))
            {
                point.Payload.Add("content", doc.Content);
            }

            // Add metadata to payload
            if (doc.Metadata != null)
            {
                foreach (var kvp in doc.Metadata)
                {
                    point.Payload.Add(kvp.Key, ConvertToValue(kvp.Value));
                }
            }

            points.Add(point);
        }

        await _client.UpsertAsync(_collectionName, points);
    }

    /// <summary>
    /// Deletes documents by their IDs.
    /// </summary>
    public void DeleteDocuments(string[] ids)
    {
        Task.Run(async () => await DeleteDocumentsAsync(ids)).Wait();
    }

    /// <summary>
    /// Deletes documents by their IDs asynchronously.
    /// </summary>
    public async Task DeleteDocumentsAsync(string[] ids)
    {
        ThrowIfCollectionNotInitialized();

        var pointIds = ids.Select(id => new PointId { Uuid = id }).ToList();
        await _client.DeleteAsync(_collectionName, pointIds);
    }

    public Task DeleteAllDocumentsAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Performs a similarity search using an embedding vector.
    /// </summary>
    public VectorDocument[] QueryByEmbedding(
        float[] embedding,
        TornadoWhereOperator? where = null,
        int topK = 5,
        bool includeScore = true)
    {
        return Task.Run(async () =>
            await QueryByEmbeddingAsync(embedding, where, topK, includeScore)).Result;
    }

    /// <summary>
    /// Performs a similarity search using an embedding vector asynchronously.
    /// </summary>
    public async Task<VectorDocument[]> QueryByEmbeddingAsync(
        float[] embedding,
        TornadoWhereOperator? where = null,
        int topK = 5,
        bool includeScore = true)
    {
        ThrowIfCollectionNotInitialized();

        Filter? filter = null;
        if (where != null)
        {
            filter = ConvertToQdrantFilter(where);
        }

        var searchResults = await _client.SearchAsync(
            _collectionName,
            embedding,
            filter: filter,
            limit: (ulong)topK,
            payloadSelector: true,
            vectorsSelector: true
        );

        return searchResults.Select(result => ConvertToVectorDocument(result, includeScore))
            .ToArray();
    }

    /// <summary>
    /// Converts a Qdrant ScoredPoint to a VectorDocument.
    /// </summary>
    private VectorDocument ConvertToVectorDocument(ScoredPoint scoredPoint, bool includeScore = true)
    {
        var id = scoredPoint.Id.Uuid;
        var embedding = scoredPoint.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>();
        var content = scoredPoint.Payload.ContainsKey("content")
            ? scoredPoint.Payload["content"].StringValue
            : string.Empty;

        var metadata = new Dictionary<string, object>();
        foreach (var kvp in scoredPoint.Payload)
        {
            if (kvp.Key != "content")
            {
                metadata[kvp.Key] = ConvertFromValue(kvp.Value);
            }
        }

        float? score = includeScore ? scoredPoint.Score : null;

        return new VectorDocument(id, content, metadata, embedding, score);
    }

    /// <summary>
    /// Converts a Qdrant RetrievedPoint to a VectorDocument.
    /// </summary>
    private VectorDocument ConvertToVectorDocument(RetrievedPoint retrievedPoint)
    {
        var id = retrievedPoint.Id.Uuid;
        var embedding = retrievedPoint.Vectors?.Vector?.Data?.ToArray() ?? Array.Empty<float>();
        var content = retrievedPoint.Payload.ContainsKey("content")
            ? retrievedPoint.Payload["content"].StringValue
            : string.Empty;

        var metadata = new Dictionary<string, object>();
        foreach (var kvp in retrievedPoint.Payload)
        {
            if (kvp.Key != "content")
            {
                metadata[kvp.Key] = ConvertFromValue(kvp.Value);
            }
        }

        return new VectorDocument(id, content, metadata, embedding);
    }

    /// <summary>
    /// Converts a TornadoWhereOperator to a Qdrant Filter.
    /// </summary>
    private Filter? ConvertToQdrantFilter(TornadoWhereOperator whereOperator)
    {
        var whereDict = whereOperator.ToWhere();
        if (whereDict == null || whereDict.Count == 0)
        {
            return null;
        }

        var filter = new Filter();
        var conditions = new List<Condition>();

        foreach (var kvp in whereDict)
        {
            if (kvp.Key == "$and" && kvp.Value is object[] andValues)
            {
                // Handle AND operator
                var must = new List<Condition>();
                foreach (var item in andValues)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        must.AddRange(ConvertDictionaryToConditions(dict));
                    }
                }
                filter.Must.AddRange(must);
            }
            else if (kvp.Key == "$or" && kvp.Value is object[] orValues)
            {
                // Handle OR operator
                var should = new List<Condition>();
                foreach (var item in orValues)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        should.AddRange(ConvertDictionaryToConditions(dict));
                    }
                }
                filter.Should.AddRange(should);
            }
            else
            {
                // Handle simple field condition
                conditions.AddRange(ConvertDictionaryToConditions(whereDict));
                break;
            }
        }

        if (conditions.Count > 0)
        {
            filter.Must.AddRange(conditions);
        }

        return filter;
    }

    /// <summary>
    /// Converts a dictionary to Qdrant conditions.
    /// </summary>
    private List<Condition> ConvertDictionaryToConditions(Dictionary<string, object> dict)
    {
        var conditions = new List<Condition>();

        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> opDict)
            {
                foreach (var op in opDict)
                {
                    Condition? condition = null;

                    switch (op.Key)
                    {
                        case "$eq":
                            condition = CreateMatchCondition(kvp.Key, op.Value);
                            break;
                        case "$ne":
                            // For not-equal, we'll skip it for now as it requires must_not logic
                            break;
                        case "$gt":
                            condition = Range(kvp.Key, new QdrantRange
                            {
                                Gt = ConvertToDouble(op.Value)
                            });
                            break;
                        case "$gte":
                            condition = Range(kvp.Key, new QdrantRange
                            {
                                Gte = ConvertToDouble(op.Value)
                            });
                            break;
                        case "$lt":
                            condition = Range(kvp.Key, new QdrantRange
                            {
                                Lt = ConvertToDouble(op.Value)
                            });
                            break;
                        case "$lte":
                            condition = Range(kvp.Key, new QdrantRange
                            {
                                Lte = ConvertToDouble(op.Value)
                            });
                            break;
                        case "$in":
                            if (op.Value is object[] values && values.Length > 0)
                            {
                                // Try to match strings or integers
                                if (values[0] is string)
                                {
                                    condition = Match(kvp.Key, values.Select(v => v.ToString() ?? string.Empty).ToList());
                                }
                                else if (values[0] is int || values[0] is long)
                                {
                                    condition = Match(kvp.Key, values.Select(v => Convert.ToInt64(v)).ToList());
                                }
                            }
                            break;
                    }

                    if (condition != null)
                    {
                        conditions.Add(condition);
                    }
                }
            }
        }

        return conditions;
    }

    /// <summary>
    /// Creates a Match condition based on the value type.
    /// </summary>
    private Condition CreateMatchCondition(string key, object value)
    {
        return value switch
        {
            string str => MatchKeyword(key, str),
            int i => Match(key, (long)i),
            long l => Match(key, l),
            bool b => Match(key, b),
            _ => MatchKeyword(key, value.ToString() ?? string.Empty)
        };
    }

    /// <summary>
    /// Converts a C# object to a Qdrant Value.
    /// </summary>
    private static Value ConvertToValue(object obj)
    {
        var value = new Value();

        switch (obj)
        {
            case null:
                value.NullValue = NullValue.NullValue;
                break;
            case string str:
                value.StringValue = str;
                break;
            case int i:
                value.IntegerValue = i;
                break;
            case long l:
                value.IntegerValue = l;
                break;
            case double d:
                value.DoubleValue = d;
                break;
            case float f:
                value.DoubleValue = f;
                break;
            case bool b:
                value.BoolValue = b;
                break;
            default:
                value.StringValue = obj.ToString() ?? string.Empty;
                break;
        }

        return value;
    }

    /// <summary>
    /// Converts a Qdrant Value to a C# object.
    /// </summary>
    private static object ConvertFromValue(Value value)
    {
        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.IntegerValue => value.IntegerValue,
            Value.KindOneofCase.DoubleValue => value.DoubleValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            Value.KindOneofCase.NullValue => null!,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Converts an object to double for range queries.
    /// </summary>
    private static double ConvertToDouble(object obj)
    {
        return obj switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            _ => Convert.ToDouble(obj)
        };
    }
}
