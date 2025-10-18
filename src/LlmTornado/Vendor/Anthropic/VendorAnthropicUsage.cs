using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Vendor.Anthropic;

/// <summary>
/// Usage from Anthropic.
/// </summary>
public class VendorAnthropicUsage : IChatUsage
{
    /// <summary>
    /// Input tokens.
    /// </summary>
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }
    
    /// <summary>
    /// Output tokens.
    /// </summary>
    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// Cache creation input tokens.
    /// </summary>
    [JsonProperty("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }
    
    /// <summary>
    /// Cache read input tokens.
    /// </summary>
    [JsonProperty("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }
    
    /// <summary>
    /// Cache creation detail.
    /// </summary>
    [JsonProperty("cache_creation")]
    public VendorAnthropicUsageCacheCreation? CacheCreation { get; set; }

    [JsonProperty("service_tier")]
    public string? ServiceTier { get; set; }

    [JsonProperty("server_tool_use")]
    public VendorAnthropicServerUsage? ServerToolUsage { get; set; }

}

public class VendorAnthropicServerUsage
{
    /// <summary>
    /// Usage details.
    /// </summary>
    [JsonProperty("web_search_requests")]
    public int WebSearchRequests { get; set; }

    [JsonProperty("web_fetch_requests")]
    public int WebFetchRequests { get; set; }
}

/// <summary>
/// Cache creation details.
/// </summary>
public class VendorAnthropicUsageCacheCreation
{
    /// <summary>
    /// Input tokens for 5m ttl cache.
    /// </summary>
    [JsonProperty("ephemeral_5m_input_tokens")]
    public int Ephemeral5MInputTokens { get; set; }
    
    /// <summary>
    /// Input tokens for 1h ttl cache.
    /// </summary>
    [JsonProperty("ephemeral_1h_input_tokens")]
    public int Ephemeral1HInputTokens { get; set; }
}

