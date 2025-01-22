using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents a vector store object which is a collection of processed files 
/// that can be used by the `file_search` tool.
/// </summary>
public class VectorStore : ApiResultBase
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The Unix timestamp (in seconds) for when the vector store was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    /// <summary>
    /// The name of the vector store.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The total number of bytes used by the files in the vector store.
    /// </summary>
    [JsonPropertyName("usage_bytes")]
    public long UsageBytes { get; set; }

    /// <summary>
    /// Counts of files in different processing states.
    /// </summary>
    [JsonPropertyName("file_counts")]
    public FileCountInfo FileCounts { get; set; } = null!;

    /// <summary>
    /// The status of the vector store, which can be either `expired`, `in_progress`, or `completed`.
    /// A status of `completed` indicates that the vector store is ready for use.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// The expiration policy for the vector store.
    /// </summary>
    [JsonPropertyName("expires_after")]
    public ExpirationPolicy? ExpiresAfter { get; set; }

    /// <summary>
    /// The Unix timestamp (in seconds) for when the vector store will expire.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// The Unix timestamp (in seconds) for when the vector store was last active.
    /// </summary>
    [JsonPropertyName("last_active_at")]
    public long? LastActiveAt { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to an object.
    /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// Maximum of 16 key-value pairs.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}