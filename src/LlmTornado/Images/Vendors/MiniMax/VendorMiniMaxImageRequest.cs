using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Images.Vendors.MiniMax;

/// <summary>
/// MiniMax-specific image generation request format.
/// </summary>
internal class VendorMiniMaxImageRequest
{
    [JsonProperty("model")]
    public string? Model { get; set; }
    
    [JsonProperty("prompt")]
    public string? Prompt { get; set; }
    
    [JsonProperty("n")]
    public int? N { get; set; }
    
    [JsonProperty("aspect_ratio")]
    public string? AspectRatio { get; set; }
    
    [JsonProperty("width")]
    public int? Width { get; set; }
    
    [JsonProperty("height")]
    public int? Height { get; set; }
    
    [JsonProperty("response_format")]
    public string? ResponseFormat { get; set; }
    
    [JsonProperty("seed")]
    public long? Seed { get; set; }
    
    [JsonProperty("prompt_optimizer")]
    public bool? PromptOptimizer { get; set; }
    
    [JsonProperty("subject_reference")]
    public List<VendorMiniMaxSubjectReference>? SubjectReference { get; set; }

    /// <summary>
    /// Creates a new MiniMax image request from a harmonized ImageGenerationRequest.
    /// </summary>
    public VendorMiniMaxImageRequest(ImageGenerationRequest request, IEndpointProvider provider)
    {
        Model = request.Model?.GetApiName;
        Prompt = request.Prompt;
        N = request.NumOfImages;
        
        ImageGenerationRequestMiniMaxExtensions? ext = request.VendorExtensions?.MiniMax;
        
        // Map response format
        if (request.ResponseFormat.HasValue)
        {
            ResponseFormat = request.ResponseFormat.Value switch
            {
                TornadoImageResponseFormats.Base64 => "base64",
                TornadoImageResponseFormats.Url => "url",
                _ => null
            };
        }
        
        // Map aspect ratio from MiniMax extension (takes priority)
        if (ext?.AspectRatio is not null)
        {
            AspectRatio = ext.AspectRatio.Value switch
            {
                ImageAspectRatio.Square => "1:1",
                ImageAspectRatio.Landscape16x9 => "16:9",
                ImageAspectRatio.Landscape4x3 => "4:3",
                ImageAspectRatio.Landscape3x2 => "3:2",
                ImageAspectRatio.Portrait2x3 => "2:3",
                ImageAspectRatio.Portrait3x4 => "3:4",
                ImageAspectRatio.Portrait9x16 => "9:16",
                ImageAspectRatio.Landscape21x9 => "21:9",
                _ => null
            };
        }
        else if (request.Size.HasValue)
        {
            // Fallback: map standard sizes to aspect ratios
            AspectRatio = request.Size.Value switch
            {
                TornadoImageSizes.Size256x256 or TornadoImageSizes.Size512x512 or TornadoImageSizes.Size1024x1024 => "1:1",
                TornadoImageSizes.Size896x1280 => "3:4",
                TornadoImageSizes.Size1280x896 => "4:3",
                TornadoImageSizes.Size768x1408 or TornadoImageSizes.Size1024x1792 => "9:16",
                TornadoImageSizes.Size1408x768 or TornadoImageSizes.Size1792x1024 => "16:9",
                TornadoImageSizes.Size1024x1536 => "2:3",
                TornadoImageSizes.Size1536x1024 => "3:2",
                TornadoImageSizes.Custom when request.Width.HasValue && request.Height.HasValue => null,
                _ => null
            };
            
            // If Custom size, pass width/height directly
            if (request.Size.Value is TornadoImageSizes.Custom && request.Width.HasValue && request.Height.HasValue)
            {
                Width = request.Width;
                Height = request.Height;
            }
        }
        else if (request.Width.HasValue && request.Height.HasValue)
        {
            // Direct width/height without size enum
            Width = request.Width;
            Height = request.Height;
        }
        
        // Map MiniMax-specific extensions
        if (ext is not null)
        {
            Seed = ext.Seed;
            PromptOptimizer = ext.PromptOptimizer;
            
            if (ext.SubjectReferences is { Count: > 0 })
            {
                SubjectReference = ext.SubjectReferences.Select(x => new VendorMiniMaxSubjectReference
                {
                    Type = x.Type,
                    ImageFile = x.ImageFile
                }).ToList();
            }
        }
    }
}

/// <summary>
/// MiniMax subject reference for image-to-image generation.
/// </summary>
internal class VendorMiniMaxSubjectReference
{
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    [JsonProperty("image_file")]
    public string? ImageFile { get; set; }
}
