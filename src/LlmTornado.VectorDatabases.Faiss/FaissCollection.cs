namespace LlmTornado.VectorDatabases.Faiss;

/// <summary>
/// Represents a FAISS collection with its metadata.
/// </summary>
public class FaissCollection
{
    /// <summary>
    /// Name of the collection.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Dimension of vectors in this collection.
    /// </summary>
    public int VectorDimension { get; set; }
    
    /// <summary>
    /// Metadata associated with the collection.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    public FaissCollection(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        Name = name;
        VectorDimension = vectorDimension;
        Metadata = metadata;
    }
}
