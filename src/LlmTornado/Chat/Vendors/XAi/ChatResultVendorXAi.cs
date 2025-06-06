using System.Collections.Generic;
using LlmTornado.Chat.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.XAi;

internal class ChatResultVendorXAi : ChatResult
{
    [JsonProperty("citations")]
    public List<string>? Citations { get; set; }

    public static ChatResult? Deserialize(string json)
    {
        ChatResultVendorXAi? resultEx = JsonConvert.DeserializeObject<ChatResultVendorXAi>(json);

        if (resultEx is not null)
        {
            resultEx.VendorExtensions = new ChatResponseVendorExtensions
            {
                XAi = new ChatResponseVendorXAiExtensions
                {
                    Citations = resultEx.Citations
                }
            };
        }
        
        return resultEx;
    }
}