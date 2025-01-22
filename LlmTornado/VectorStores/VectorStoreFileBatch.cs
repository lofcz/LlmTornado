using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents a batch of files attached to a vector store
/// </summary>
public class VectorStoreFileBatch : ApiResultBase
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The Unix timestamp (in seconds) for when the vector store files batch was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt
    {
        get => CreatedUnixTime ?? 0;
        set => CreatedUnixTime = value;
    }

    /// <summary>
    /// The ID of the vector store that the File is attached to.
    /// </summary>
    [JsonProperty("vector_store_id")]
    public string VectorStoreId { get; set; } = null!;

    /// <summary>
    /// The status of the vector store files batch, which can be either 
    /// `in_progress`, `completed`, `cancelled` or `failed`.
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Counts of files in different processing states.
    /// </summary>
    [JsonProperty("file_counts")]
    public FileCountInfo FileCounts { get; set; } = null!;
}