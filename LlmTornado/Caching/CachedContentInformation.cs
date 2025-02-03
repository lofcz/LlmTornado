using System;
using LlmTornado.Caching.Vendors.Google;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Caching;

/// <summary>
/// Information about cached resource.
/// </summary>
public class CachedContentInformation
{
    /// <summary>
    /// Date the resource was created.
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Date the resource was last updated.
    /// </summary>
    public DateTime UpdateTime { get; set; }
    
    /// <summary>
    /// Date the resource expires.
    /// </summary>
    public DateTime ExpireTime { get; set; }
    
    /// <summary>
    /// Metadata for the last operation
    /// </summary>
    public CachedContentUsage? UsageMetadata { get; set; }

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
    
    internal static CachedContentInformation? Deserialize(LLmProviders provider, string jsonData, string? postData)
    {
        return provider switch
        {
            LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleCachingCachedContentInfo>(jsonData)?.ToCreatedCachedContent(),
            _ => JsonConvert.DeserializeObject<CachedContentInformation>(jsonData)
        };
    }
}