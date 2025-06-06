using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Caching.Vendors.Google;

internal class VendorGoogleCachingCachedContentInfo
{
    [JsonProperty("createTime")]
    public DateTime CreateTime { get; set; }
    
    [JsonProperty("updateTime")]
    public DateTime UpdateTime { get; set; }
    
    [JsonProperty("expireTime")]
    public DateTime ExpireTime { get; set; }
    
    [JsonProperty("usageMetadata")]
    public CachedContentUsage UsageMetadata { get; set; }

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
    
    public CachedContentInformation ToCreatedCachedContent()
    {
        return new CachedContentInformation
        {
            CreateTime = CreateTime,
            UpdateTime = UpdateTime,
            ExpireTime = ExpireTime,
            DisplayName = DisplayName,
            Name = Name,
            UsageMetadata = UsageMetadata
        };
    }
    
    public VendorGoogleCachingCachedContentInfo()
    {
        
    }
}