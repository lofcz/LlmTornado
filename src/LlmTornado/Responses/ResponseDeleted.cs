using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Information about a deleted response.
/// </summary>
public class ResponseDeleted
{
    /// <summary>
    /// ID of the response.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Always "response".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "response";
    
    /// <summary>
    /// Whether the response was deleted.
    /// </summary>
    [JsonProperty("deleted")]
    public bool Deleted { get; set; }
}