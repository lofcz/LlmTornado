using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
///     A request to create a file batch in a vector store
/// </summary>
public class CreateVectorStoreFileBatchRequest
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("file_ids")]
    public IReadOnlyList<string> FileIds { get; set; } = null!;

    /// <summary>
    /// The strategy used to chunk the file.
    /// </summary>
    [JsonProperty("chunking_strategy")]
    public ChunkingStrategy? ChunkingStrategy { get; set; }
}