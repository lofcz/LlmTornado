using System.Collections.Generic;
using LlmTornado.Caching.Vendors.Google;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Caching;

/// <summary>
/// A paging list of cached contents.
/// </summary>
public class CachedContentList
{
    /// <summary>
    /// List of cached contents.
    /// </summary>
    public List<CachedContentInformation> CachedContents { get; set; } = [];
    
    /// <summary>
    /// A token, which can be sent as pageToken to retrieve the next page. If this field is omitted, there are no subsequent pages.
    /// </summary>
    public string? NextPageToken { get; set; }
    
    internal static CachedContentList? Deserialize(LLmProviders provider, string jsonData, string? postData)
    {
        return provider switch
        {
            LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleCachingCachedContentList>(jsonData)?.ToCachedContentList(),
            _ => JsonConvert.DeserializeObject<CachedContentList>(jsonData)
        };
    }
}