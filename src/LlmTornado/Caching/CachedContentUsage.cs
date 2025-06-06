using Newtonsoft.Json;

namespace LlmTornado.Caching;

/// <summary>
/// Usage information for cached resource.
/// </summary>
public class CachedContentUsage
{
    /// <summary>
    /// Total number of tokens that the cached content consumes.
    /// </summary>
    [JsonProperty("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}