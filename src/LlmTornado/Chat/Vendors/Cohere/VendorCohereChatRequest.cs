using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Cohere;

internal class VendorCohereChatRequest
{
    internal class VendorCohereChatRequestData : ChatRequest
    {
        [JsonProperty("tool_choice")]
        public string? ToolChoice { get; set; }
        
        [JsonProperty("safe_prompt")]
        public bool? SafePrompt { get; set; }
        
        [JsonProperty("random_seed")]
        public int? RandomSeed { get; set; }
        
        public VendorCohereChatRequestData(ChatRequest request) : base(request)
        {
            
        }
    }
    
    public VendorCohereChatRequestData? ExtendedRequest { get; set; }
    public ChatRequest? NativeRequest { get; set; }
    
    [JsonIgnore]
    public ChatMessage? TempMessage { get; set; }
    
    [JsonIgnore]
    public ChatRequest SourceRequest { get; set; }

    public VendorCohereChatRequest()
    {
        
    }
    
    public JObject Serialize(JsonSerializerSettings settings)
    {
        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
        JObject jsonPayload = JObject.FromObject(ExtendedRequest ?? NativeRequest!, serializer);
        
        if (TempMessage is not null)
        {
            SourceRequest.Messages?.Remove(TempMessage);
        }
        
        return jsonPayload;
    }
    
    public VendorCohereChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        // not supported
        request.StreamOptions = null;
        
        SourceRequest = request;
        ChatRequestVendorCohereExtensions? extensions = request.VendorExtensions?.Cohere;

        if (extensions is not null)
        {
            ExtendedRequest = new VendorCohereChatRequestData(request);
            
            /*if (extensions.SafePrompt is not null)
            {
                ExtendedRequest.SafePrompt = extensions.SafePrompt;
            }*/
        }
        else
        {
            NativeRequest = request;
        }

        if (request.ToolChoice is not null)
        {
            string? toolChoice = request.ToolChoice.Mode switch
            {
                OutboundToolChoiceModes.None => "NONE",
                OutboundToolChoiceModes.Required => "REQUIRED",
                OutboundToolChoiceModes.ToolFunction => "REQUIRED",
                _ => null
            };

            if (toolChoice is not null)
            {
                ExtendedRequest ??= new VendorCohereChatRequestData(request);
                ExtendedRequest.ToolChoice = toolChoice;
            }
        }
    }
}