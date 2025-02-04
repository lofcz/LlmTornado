using System.Collections.Generic;
using LlmTornado.Caching;
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
    
    [JsonIgnore]
    internal CachedContentInformation? CachedContentInformation { get; set; }
    
    /// <summary>
    /// Forces given response schema. Normally, use strict functions to automatically set this. Manually setting this is required for cached functions.
    /// </summary>
    [JsonIgnore]
    public object? ResponseSchema { get; set; }
    
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
    
    /// <summary>
    /// Cached content will be used for responses.
    /// </summary>
    /// <param name="cachedContent"></param>
    public ChatRequestVendorGoogleExtensions(CachedContentInformation cachedContent)
    {
        CachedContent = cachedContent.Name;
        CachedContentInformation = cachedContent;
    }
}