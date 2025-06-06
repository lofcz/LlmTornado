using Newtonsoft.Json;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents an error that occurred during vector store file processing
/// </summary>
public class VectorStoreFileError
{
    /// <summary>
    /// One of `server_error` or `rate_limit_exceeded`.
    /// </summary>
    [JsonProperty("code")]
    public string Code { get; set; } = null!;

    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = null!;
}