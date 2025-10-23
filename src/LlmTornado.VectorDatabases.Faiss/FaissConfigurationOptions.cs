namespace LlmTornado.VectorDatabases.Faiss;

/// <summary>
/// Configuration options for the FAISS client.
/// </summary>
public class FaissConfigurationOptions
{
    /// <summary>
    /// Directory path where FAISS indexes are stored.
    /// </summary>
    public string IndexDirectory { get; set; }

    /// <summary>
    /// Initializes a new instance of FaissConfigurationOptions.
    /// </summary>
    /// <param name="indexDirectory">Directory path where FAISS indexes will be stored. Defaults to "./faiss_indexes".</param>
    public FaissConfigurationOptions(string? indexDirectory = null)
    {
        IndexDirectory = indexDirectory ?? "./faiss_indexes";
    }
}
