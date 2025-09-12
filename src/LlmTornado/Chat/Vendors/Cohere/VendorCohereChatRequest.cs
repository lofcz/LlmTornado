using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Cohere;

internal class VendorCohereChatRequest
{
    private static readonly HashSet<ChatModel> Gen1Models = [ 
        ChatModel.Cohere.Command.Default, 
        ChatModel.Cohere.Command.CommandLight, 
        ChatModel.Cohere.Command.RPlus
    ];
    
    internal class VendorCohereChatRequestData : ChatRequest
    {
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
    }
 }