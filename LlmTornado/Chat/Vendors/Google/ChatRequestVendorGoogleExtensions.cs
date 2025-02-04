using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Google;

/// <summary>
/// Chat features supported only by Googly.
/// </summary>
public class ChatRequestVendorGoogleExtensions
{
    /// <summary>
    /// The name of the content cached to use as context to serve the prediction. Format: cachedContents/{cachedContent}
    /// </summary>
    [JsonProperty("cachedContent")]
    public string? CachedContent { get; set; }
    
    /// <summary>
    /// Empty Google extensions.
    /// </summary>
    public ChatRequestVendorGoogleExtensions()
    {
        
    }

    /// <summary>
    /// Cached content will be used for responses.
    /// </summary>
    /// <param name="cachedContent"></param>
    public ChatRequestVendorGoogleExtensions(string cachedContent)
    {
        CachedContent = cachedContent;
    }
}