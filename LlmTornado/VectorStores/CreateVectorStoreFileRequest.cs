using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
///     A request to create a file in a vector store
/// </summary>
public class CreateVectorStoreFileRequest
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("file_id")]
    public string FileId { get; set; } = null!;

    /// <summary>
    /// The strategy used to chunk the file.
    /// </summary>
    [JsonProperty("chunking_strategy")]
    public ChunkingStrategy? ChunkingStrategy { get; set; } = null!;
}