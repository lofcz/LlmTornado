using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Zai;

/// <summary>
/// https://docs.z.ai/api-reference/introduction
/// </summary>
internal class VendorZaiChatRequest
{
    public VendorZaiChatRequestData? ExtendedRequest { get; set; }
    public ChatRequest? NativeRequest { get; set; }
    
    [JsonIgnore]
    public ChatRequest SourceRequest { get; set; }
    
    public JObject Serialize(JsonSerializerSettings settings)
    {
        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
        JObject jsonPayload = JObject.FromObject(ExtendedRequest ?? NativeRequest, serializer);
        
        return jsonPayload;
    }
    
    public VendorZaiChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        SourceRequest = request;
        ChatRequestVendorZaiExtensions? extensions = request.VendorExtensions?.Zai;

        if (extensions is not null)
        {
            ExtendedRequest = new VendorZaiChatRequestData(request);
            
            if (extensions.DoSample is not null)
            {
                ExtendedRequest.DoSample = extensions.DoSample;
            }

            if (extensions.RequestId is not null)
            {
                ExtendedRequest.RequestId = extensions.RequestId;
            }

            if (extensions.ToolStream is not null)
            {
                ExtendedRequest.ToolStream = extensions.ToolStream;
            }
        }
        else
        {
            NativeRequest = request;
        }
    }
}
