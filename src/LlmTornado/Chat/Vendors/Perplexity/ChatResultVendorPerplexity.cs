using LlmTornado.Chat.Vendors.XAi;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Perplexity;

internal class ChatResultVendorPerplexity : ChatResult
{
    [JsonProperty("usage")]
    public new VendorPerplexityUsage Usage { get; set; }
    
    public static ChatResult? Deserialize(string json)
    {
        ChatResultVendorPerplexity? resultEx = JsonConvert.DeserializeObject<ChatResultVendorPerplexity>(json);

        if (resultEx is null)
        {
            return null;
        }
        
        ChatResult result = new ChatResult(resultEx)
        {
            Usage = new ChatUsage(resultEx.Usage)
        };

        return result;
    }
}