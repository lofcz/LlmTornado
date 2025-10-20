using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Alibaba;

internal class VendorAlibabaChatRequest
{
    public VendorAlibabaChatRequestData? ExtendedRequest { get; set; }
    public ChatRequest? NativeRequest { get; set; }
    
    [JsonIgnore]
    public ChatRequest SourceRequest { get; set; }
    
    public JObject Serialize(JsonSerializerSettings settings)
    {
        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
        JObject jsonPayload = JObject.FromObject(ExtendedRequest ?? NativeRequest, serializer);
        
        return jsonPayload;
    }
    
    public VendorAlibabaChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        SourceRequest = request;
        
        // For now, Alibaba uses standard OpenAI-compatible format
        // No special extensions needed yet
        NativeRequest = request;
    }
}

internal class VendorAlibabaChatRequestData : ChatRequest
{
    public VendorAlibabaChatRequestData(ChatRequest request) : base(request)
    {
        // Alibaba-specific modifications can be added here
    }
}
