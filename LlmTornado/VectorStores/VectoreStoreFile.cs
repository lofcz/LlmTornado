using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents a file attached to a vector store
/// </summary>
public class VectorStoreFile : ApiResultBase
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The total vector store usage in bytes. 
    /// Note that this may be different from the original file size.
    /// </summary>
    [JsonProperty("usage_bytes")]
    public long UsageBytes { get; set; }

    /// <summary>
    /// The Unix timestamp (in seconds) for when the vector store file was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// The ID of the vector store that the File is attached to.
    /// </summary>
    [JsonProperty("vector_store_id")]
    public string VectorStoreId { get; set; } = null!;

    /// <summary>
    /// The status of the vector store file, which can be either `in_progress`, 
    /// `completed`, `cancelled`, or `failed`. The status `completed` indicates 
    /// that the vector store file is ready for use.
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// The last error associated with this vector store file. 
    /// Will be `null` if there are no errors.
    /// </summary>
    [JsonProperty("last_error")]
    public VectorStoreFileError? LastError { get; set; }

    /// <summary>
    /// The strategy used to chunk the file.
    /// </summary>
    [JsonProperty("chunking_strategy")]
    [JsonConverter(typeof(ChunkingStrategyConverter))]
    public ChunkingStrategy? ChunkingStrategy { get; set; } = null!;
}