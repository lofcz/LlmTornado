using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Mistral;

/// <summary>
/// https://docs.mistral.ai/api/#tag/chat/operation/chat_completion_v1_chat_completions_post
/// </summary>
internal class VendorMistralChatRequest
{
    public VendorMistralChatRequestData? ExtendedRequest { get; set; }
    public ChatRequest? NativeRequest { get; set; }
    
    [JsonIgnore]
    public ChatMessage? TempMessage { get; set; }
    
    [JsonIgnore]
    public ChatRequest SourceRequest { get; set; }
    
    public string Serialize()
    {
        string serialized = JsonConvert.SerializeObject(ExtendedRequest ?? NativeRequest, EndpointBase.NullSettings);

        if (TempMessage is not null)
        {
            SourceRequest.Messages?.Remove(TempMessage);
        }
        
        return serialized;
    }
    
    internal class VendorMistralChatRequestData : ChatRequest
    {
        [JsonProperty("safe_prompt")]
        public bool? SafePrompt { get; set; }

        [JsonProperty("prediction")]
        public Prediction? Prediction { get; set; }
        
        [JsonProperty("random_seed")]
        public int? RandomSeed { get; set; }
        
        public VendorMistralChatRequestData(ChatRequest request) : base(request)
        {
            
        }
    }

    internal class Prediction
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "content";
        
        [JsonProperty("content")]
        public string Content { get; set; }
    }
    
    public VendorMistralChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        // not supported
        request.StreamOptions = null;
        
        SourceRequest = request;
        ChatRequestVendorMistralExtensions? extensions = request.VendorExtensions?.Mistral;

        if (extensions is not null)
        {
            ExtendedRequest = new VendorMistralChatRequestData(request);
            
            if (extensions.SafePrompt is not null)
            {
                ExtendedRequest.SafePrompt = extensions.SafePrompt;
            }

            if (extensions.Prediction is not null)
            {
                ExtendedRequest.Prediction = new Prediction
                {
                    Content = extensions.Prediction
                };
            }

            ExtendedRequest.RandomSeed = extensions.RandomSeed;

            if (extensions.Prefix is not null)
            {
                ChatMessage? lastMessage = request.Messages?.LastOrDefault();

                if (lastMessage?.Role is ChatMessageRoles.User)
                {
                    TempMessage = new ChatMessage(ChatMessageRoles.Assistant, extensions.Prefix)
                    {
                        Prefix = true
                    };
                    request.Messages?.Add(TempMessage);   
                }
            }
        }
        else
        {
            NativeRequest = request;
        }
    }
}