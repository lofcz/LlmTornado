using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Zai;

/// <summary>
/// Chat response features specific to ZAI.
/// </summary>
public class ChatResponseVendorZaiExtensions
{
    /// <summary>
    /// Web search results returned by the model when web search tools are used.
    /// </summary>
    public List<VendorZaiWebSearchResult>? WebSearchResults { get; set; }
}

/// <summary>
/// A single web search result item from ZAI's response.
/// </summary>
public class VendorZaiWebSearchResult
{
    /// <summary>
    /// Title of the search result.
    /// </summary>
    [JsonProperty("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Content summary.
    /// </summary>
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Result URL.
    /// </summary>
    [JsonProperty("link")]
    public string? Link { get; set; }

    /// <summary>
    /// Website name.
    /// </summary>
    [JsonProperty("media")]
    public string? Media { get; set; }

    /// <summary>
    /// Website icon URL.
    /// </summary>
    [JsonProperty("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Index number reference.
    /// </summary>
    [JsonProperty("refer")]
    public string? Refer { get; set; }

    /// <summary>
    /// Website publication date.
    /// </summary>
    [JsonProperty("publish_date")]
    public string? PublishDate { get; set; }
}
