using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LlmTornado.Chat.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Caching.Vendors.Google;

internal class VendorGoogleCachingCreateCachedContentRequest
{
    /// <summary>
    /// How many seconds should be the resource cached.
    /// </summary>
    [JsonProperty("ttl")]
    public string Ttl { get; set; }

    /// <summary>
    /// The resource name referring to the cached content. Format: cachedContents/{id}
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// The user-generated meaningful display name of the cached content. Maximum 128 Unicode characters.
    /// </summary>
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// The name of the Model to use for cached content.
    /// Context caching is only available for stable models with fixed versions (for example, gemini-1.5-pro-001). You must include the version postfix (for example, the -001 in gemini-1.5-pro-001).
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }
    
    /// <summary>
    /// System message to cache. Currently only text-based messages are supported.
    /// </summary>
    [JsonProperty("systemInstruction")]
    public VendorGoogleCachingCachedContent? SystemInstruction { get; set; }
    
    /// <summary>
    /// The content to cache.
    /// </summary>
    [JsonProperty("contents")]
    public List<VendorGoogleCachingCachedContent>? Contents { get; set; }

    public VendorGoogleCachingCreateCachedContentRequest()
    {
        
    }

    public VendorGoogleCachingCreateCachedContentRequest(CreateCachedContentRequest request)
    {
        Ttl = $"{request.TimeToLive.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s";
        Name = request.Name;
        DisplayName = request.DisplayName;
        Model = $"models/{request.Model.Name}"; // see: https://ai.google.dev/api/caching#cache_create-SHELL
        SystemInstruction = request.System is null ? null : new VendorGoogleCachingCachedContent(request.System);
        Contents = request.Contents?.Select(x => new VendorGoogleCachingCachedContent(x)).ToList();
    }
}