using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Zai;

internal class ChatResultVendorZai : ChatResult
{
    /// <summary>
    /// Web search results returned by ZAI when web search tools are used.
    /// </summary>
    [JsonProperty("web_search")]
    public List<VendorZaiWebSearchResult>? WebSearch { get; set; }

    public static ChatResult? Deserialize(string json)
    {
        ChatResultVendorZai? resultEx = JsonConvert.DeserializeObject<ChatResultVendorZai>(json);

        if (resultEx is not null && resultEx.WebSearch is { Count: > 0 })
        {
            resultEx.VendorExtensions = new ChatResponseVendorExtensions
            {
                Zai = new ChatResponseVendorZaiExtensions
                {
                    WebSearchResults = resultEx.WebSearch
                }
            };
        }
        
        return resultEx;
    }
}
