using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Images.Vendors.MiniMax;

/// <summary>
/// MiniMax-specific image generation response.
/// Response format differs from OpenAI: data contains image_urls/image_base64 arrays instead of objects.
/// </summary>
internal class VendorMiniMaxImageResult : VendorImageGenerationResult
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    
    [JsonProperty("data")]
    public VendorMiniMaxImageData? Data { get; set; }
    
    [JsonProperty("metadata")]
    public VendorMiniMaxImageMetadata? Metadata { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxImageBaseResp? BaseResp { get; set; }
    
    public override ImageGenerationResult ToChatResult(string? postData)
    {
        List<TornadoGeneratedImage> images = [];
        
        if (Data?.ImageUrls is { Count: > 0 })
        {
            images.AddRange(Data.ImageUrls.Select(url => new TornadoGeneratedImage { Url = url }));
        }
        else if (Data?.ImageBase64 is { Count: > 0 })
        {
            images.AddRange(Data.ImageBase64.Select(b64 => new TornadoGeneratedImage { Base64 = b64 }));
        }
        
        return new ImageGenerationResult
        {
            Data = images
        };
    }
}

internal class VendorMiniMaxImageData
{
    [JsonProperty("image_urls")]
    public List<string>? ImageUrls { get; set; }
    
    [JsonProperty("image_base64")]
    public List<string>? ImageBase64 { get; set; }
}

internal class VendorMiniMaxImageMetadata
{
    [JsonProperty("success_count")]
    public int? SuccessCount { get; set; }
    
    [JsonProperty("failed_count")]
    public int? FailedCount { get; set; }
}

internal class VendorMiniMaxImageBaseResp
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    [JsonProperty("status_msg")]
    public string? StatusMsg { get; set; }
}
