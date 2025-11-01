namespace LlmTornado.VectorDatabases;

/// <summary>
/// Vector document with content, metadata, and embedding.
/// Used to represent documents stored in or retrieved from a vector database.
/// </summary>
public class VectorDocument
{
    /// <summary>
    /// ID of the document.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Content stored in the document.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Metadata associated with the document.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    /// <summary>
    /// Vector embedding representing the document in vector space.
    /// </summary>
    public float[]? Embedding { get; set; }
    /// <summary>
    /// Dimension of the embedding vector.
    /// </summary>
    public int Dimension => Embedding?.Length ?? 0;
    /// <summary>
    /// Queried relevance score for the document.
    /// </summary>
    public float? Score { get; set; } // Optional relevance score for query results
    public VectorDocument(string id, string content, Dictionary<string, object>? metadata = null, float[]? embedding = null, float? score = null)
    {
        Id = id;
        Content = content;
        Metadata = metadata;
        Embedding = embedding;
        Score = score;
    }

    public override string ToString()
    {
        string metadataStr = Metadata != null
            ? "{" + string.Join(", ", Metadata.Select(kv => $"{kv.Key}: {kv.Value}")) + "}"
            : "null";
        return $"VectorDocument(Id={Id},\n Content={Content},\n Metadata={metadataStr},\n Score={Score}\n)";
    }
}
