using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Images.Vendors.Google;

internal class VendorGoogleImageRequest
{
    // source: https://cloud.google.com/vertex-ai/generative-ai/docs/model-reference/imagen-api

    [JsonProperty("instances")]
    public List<ImageRequestInstance> Instances { get; set; }
    
    [JsonProperty("parameters")]
    public ImageRequestParameters? Parameters { get; set; }

    internal class ImageRequestInstance
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; }
    }

    internal class ImageRequestParameters
    {
        
        [JsonProperty("sampleCount")]
        public int SampleCount { get; set; } = 1;
        
        [JsonProperty("seed")]
        public uint? Seed { get; set; }
        
        /// <summary>
        /// supported only by imagen-3.0-generate-002, defaults to true there
        /// </summary>
        [JsonProperty("enhancePrompt")]
        public bool? EnhancePrompt { get; set; }
        
        /// <summary>
        /// Optional. Whether to enable the Responsible AI filtered reason code in responses with blocked input or output. Default value: false.
        /// </summary>
        [JsonProperty("includeRaiReason")]
        public bool? IncludeRaiReason { get; set; }
        
        /// <summary>
        /// supported only by imagen-3.0-generate-001 and older models
        /// </summary>
        [JsonProperty("negativePrompt")]
        public string? NegativePrompt { get; set; }
        
        /// <summary>
        /// new models support: "1:1", "9:16", "16:9", "3:4", or "4:3"
        /// </summary>
        [JsonProperty("aspectRatio")]
        public string? AspectRatio { get; set; }
        
        /// <summary>
        /// new models support: "1:1", "9:16", "16:9", "3:4", or "4:3"
        /// </summary>
        [JsonProperty("outputOptions")]
        public OutputOptionsCls? OutputOptions { get; set; }
        
        /// <summary>
        /// imagegeneration@002 only - "photograph" "digital_art" "landscape" "sketch" "watercolor" "cyberpunk" "pop_art"
        /// </summary>
        [JsonProperty("sampleImageStyle")]
        public string? SampleImageStyle { get; set; }

        /// <summary>
        /// Optional: string (imagen-3.0-generate-002, imagen-3.0-generate-001, imagen-3.0-fast-generate-001, and imagegeneration@006 only)
        /// dont_allow / allow_adult / allow_all
        /// defaults to allow_adult
        /// </summary>
        [JsonProperty("personGeneration")]
        public string? PersonGeneration { get; set; }
        
        /// <summary>
        /// Optional: string (imagen-3.0-generate-002, imagen-3.0-generate-001, imagen-3.0-fast-generate-001, and imagegeneration@006 only)
        /// block_low_and_above / block_medium_and_above / block_only_high / block_none
        /// The default value is "block_medium_and_above".
        /// </summary>
        [JsonProperty("safetySetting")]
        public string? SafetySetting { get; set; }
        
        /// <summary>
        /// Add an invisible watermark to the generated images. The default value is false for the imagegeneration@002 and imagegeneration@005 models, and true for the imagen-3.0-generate-002, imagen-3.0-generate-001, imagen-3.0-fast-generate-001, imagegeneration@006, and imagegeneration@006 models.
        /// </summary>
        [JsonProperty("addWatermark")]
        public bool? AddWatermark { get; set; }
        
        /// <summary>
        /// Cloud Storage URI to store the generated images.
        /// </summary>
        [JsonProperty("storageUri")]
        public string? StorageUri { get; set; }   
    }
    
    internal class OutputOptionsCls
    {
        /// <summary>
        /// "image/png": Save as a PNG image; "image/jpeg": Save as a JPEG image
        /// </summary>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
        
        /// <summary>
        /// The level of compression if the output type is "image/jpeg". Accepted values are 0 through 100. The default value is 75.
        /// </summary>
        [JsonProperty("compressionQuality")]
        public int? CompressionQuality { get; set; }
    }
    
    public VendorGoogleImageRequest(ImageGenerationRequest request, IEndpointProvider provider)
    {
        request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.ImageGeneration, null)}/{request.Model?.Name}:predict");

        Instances =
        [
            new ImageRequestInstance
            {
                Prompt = request.Prompt,
            }
        ];

        Parameters = new ImageRequestParameters
        {
            SampleCount = request.NumOfImages ?? 1,
            // non-enterprise customers can't turn this surveillance off, sadly
            //AddWatermark = false,
            PersonGeneration = "allow_adult", // again, we can't turn this to allow_all
            // SafetySetting = "block_only_high", // only block_low_and_above is supported -_-
            AspectRatio = request.Size switch
            {
                TornadoImageSizes.Size256x256 or TornadoImageSizes.Size512x512 or TornadoImageSizes.Size1024x1024 => "1:1",
                TornadoImageSizes.Size896x1280 => "3:4",
                TornadoImageSizes.Size1280x896 => "4:3",
                TornadoImageSizes.Size1408x768 => "16:9",
                TornadoImageSizes.Size768x1408 => "9:16",
                _ => null
            }
        };

        if (request.VendorExtensions?.Google is not null)
        {
            ImageGenerationRequestGoogleExtensions? g = request.VendorExtensions.Google;
            ImageRequestParameters p = Parameters;
            
            if (g.PersonSetting is not null)
            {
                p.PersonGeneration = g.PersonSetting switch
                {
                    ImageGenerationRequestGoogleExtensionsPersonSettings.DontAllow => "dont_allow",
                    ImageGenerationRequestGoogleExtensionsPersonSettings.AllowAdult => "allow_adult",
                    _ => p.PersonGeneration
                };
            }

            if (g.CompressionQuality is not null)
            {
                p.OutputOptions ??= new OutputOptionsCls();
                p.OutputOptions.CompressionQuality = g.CompressionQuality;
            }

            if (g.MimeType is not null)
            {
                p.OutputOptions ??= new OutputOptionsCls();
                p.OutputOptions.MimeType = g.MimeType switch
                {
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Jpeg => "image/jpeg",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Gif => "image/gif",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Png => "image/png",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Webp => "image/webp",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Bmp => "image/bmp",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Tiff => "image/tiff",
                    ImageGenerationRequestGoogleExtensionsMimeTypes.Icon => "image/vnd.microsoft.icon",
                    _ => p.OutputOptions.MimeType
                };
            }

            if (g.EnablePromptRewriting is not null)
            {
                p.EnhancePrompt = g.EnablePromptRewriting;
            }

            if (g.StorageUri is not null)
            {
                p.StorageUri = g.StorageUri;
            }

            if (g.SafetySetting is not null)
            {
                p.SafetySetting = g.SafetySetting switch
                {
                    ImageGenerationRequestGoogleExtensionsSafetySettings.BlockLowAndAbove => "block_low_and_above",
                    ImageGenerationRequestGoogleExtensionsSafetySettings.BlockMediumAndAbove => "block_medium_and_above",
                    ImageGenerationRequestGoogleExtensionsSafetySettings.BlockOnlyHigh => "block_only_high",
                    _ => p.SafetySetting
                };
            }
        }
    }
}