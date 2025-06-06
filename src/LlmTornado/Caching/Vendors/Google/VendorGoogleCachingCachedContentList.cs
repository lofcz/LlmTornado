using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Caching.Vendors.Google;

internal class VendorGoogleCachingCachedContentList
{
    [JsonProperty("nextPageToken")]
    public string? NextPageToken { get; set; }
    
    [JsonProperty("cachedContents")]
    public List<VendorGoogleCachingCachedContentInfo> CachedContents { get; set; }

    public CachedContentList ToCachedContentList()
    {
        return new CachedContentList
        {
            NextPageToken = NextPageToken,
            CachedContents = CachedContents.Select(x => x.ToCreatedCachedContent()).ToList()
        };
    }
}