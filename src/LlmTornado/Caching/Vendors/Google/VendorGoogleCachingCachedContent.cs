using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Caching.Vendors.Google;

internal class VendorGoogleCachingCachedContent
{
    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("parts")]
    public List<VendorGoogleChatRequestMessagePart> Parts { get; set; } = [];

    public VendorGoogleCachingCachedContent()
    {
        
    }

    public VendorGoogleCachingCachedContent(CachedContent content)
    {
        Role = content.Role switch
        {
            null => null,
            CachedContentRoles.Model => "model",
            _ => "user"
        };

        Parts = content.Parts.Select(x => new VendorGoogleChatRequestMessagePart(x)).ToList();
    }
}