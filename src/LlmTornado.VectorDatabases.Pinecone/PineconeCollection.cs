namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Represents a Pinecone namespace within an index.
/// In Pinecone, namespaces are used to partition vectors within an index.
/// </summary>
public class PineconeCollection
{
    /// <summary>
    /// The name of the namespace (collection).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The name of the Pinecone index containing this namespace.
    /// </summary>
    public string IndexName { get; set; }

    /// <summary>
    /// The dimension of vectors in this collection.
    /// </summary>
    public int Dimension { get; set; }

    /// <summary>
    /// Creates a new PineconeCollection instance.
    /// </summary>
    /// <param name="name">The namespace name</param>
    /// <param name="indexName">The index name</param>
    /// <param name="dimension">The vector dimension</param>
    public PineconeCollection(string name, string indexName, int dimension)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        Dimension = dimension;
    }
}

