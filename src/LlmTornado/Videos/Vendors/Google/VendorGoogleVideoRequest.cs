using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using LlmTornado.Code;
using LlmTornado.Videos.Models;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.Google;

/// <summary>
/// Google-specific video generation request.
/// </summary>
internal class VendorGoogleVideoRequest
{
    /// <summary>
    /// The instances for video generation.
    /// </summary>
    [JsonProperty("instances")]
    public List<VideoRequestInstance> Instances { get; set; }
    
    /// <summary>
    /// The parameters for video generation.
    /// </summary>
    [JsonProperty("parameters")]
    public VideoRequestParameters? Parameters { get; set; }

    /// <summary>
    /// Constructor that transforms a VideoGenerationRequest into Google's format.
    /// </summary>
    /// <param name="request">The video generation request</param>
    /// <param name="provider">The endpoint provider</param>
    public VendorGoogleVideoRequest(VideoGenerationRequest request, IEndpointProvider provider)
    {
        // Create the instance with the prompt
        VideoRequestInstance instance = new VideoRequestInstance
        {
            Prompt = request.Prompt ?? string.Empty
        };
        
        // Add image if provided
        if (request.Image is not null)
        {
            instance.Image = request.Image;
        }
        
        // Add last frame if provided
        if (request.LastFrame is not null)
        {
            instance.LastFrame = request.LastFrame;
        }
        
        // Add reference images if provided
        if (request.ReferenceImages is not null && request.ReferenceImages.Count > 0)
        {
            instance.ReferenceImages = request.ReferenceImages;
        }
        
        // Add video if provided
        if (request.Video is not null)
        {
            instance.Video = request.Video;
        }
        
        Instances = [instance];
        
        // Create parameters
        VideoRequestParameters parameters = new VideoRequestParameters();
        
        // Set common parameters
        if (!string.IsNullOrEmpty(request.NegativePrompt))
        {
            parameters.NegativePrompt = request.NegativePrompt;
        }
        
        if (request.AspectRatio.HasValue)
        {
            parameters.AspectRatio = GetEnumValue(request.AspectRatio.Value);
        }
        
        if (request.Resolution.HasValue)
        {
            parameters.Resolution = GetEnumValue(request.Resolution.Value);
        }
        
        // DurationSeconds takes precedence if set
        if (request.DurationSeconds.HasValue)
        {
            parameters.DurationSeconds = request.DurationSeconds.Value;
        }
        else if (request.Duration.HasValue && request.Duration.Value != VideoDuration.Custom)
        {
            parameters.DurationSeconds = (int)request.Duration.Value;
        }
        
        if (request.PersonGeneration.HasValue)
        {
            parameters.PersonGeneration = GetEnumValue(request.PersonGeneration.Value);
        }
        
        // Apply Google-specific extensions
        if (request.GoogleExtensions is not null)
        {
            if (request.GoogleExtensions.StorageUri is not null)
            {
                parameters.StorageUri = request.GoogleExtensions.StorageUri;
            }
            
            if (request.GoogleExtensions.AddWatermark.HasValue)
            {
                parameters.AddWatermark = request.GoogleExtensions.AddWatermark.Value;
            }
            
            if (request.GoogleExtensions.SafetySetting.HasValue)
            {
                parameters.SafetySetting = request.GoogleExtensions.SafetySetting.Value.ToString().ToLowerInvariant();
            }
        }
        
        Parameters = parameters;
    }

    /// <summary>
    /// Helper method to get the EnumMember value from an enum
    /// </summary>
    private static string GetEnumValue<T>(T enumValue) where T : Enum
    {
        FieldInfo? fieldInfo = typeof(T).GetField(enumValue.ToString());
        if (fieldInfo != null)
        {
            EnumMemberAttribute? attr = fieldInfo.GetCustomAttribute<EnumMemberAttribute>();
            if (attr != null)
            {
                return attr.Value ?? enumValue.ToString();
            }
        }
        return enumValue.ToString();
    }

    /// <summary>
    /// Represents an instance for video generation.
    /// </summary>
    internal class VideoRequestInstance
    {
        /// <summary>
        /// The prompt for video generation.
        /// </summary>
        [JsonProperty("prompt")]
        public string Prompt { get; set; }
        
        /// <summary>
        /// An initial image to animate.
        /// </summary>
        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public VideoImage? Image { get; set; }
        
        /// <summary>
        /// The final image for an interpolation video to transition.
        /// </summary>
        [JsonProperty("lastFrame", NullValueHandling = NullValueHandling.Ignore)]
        public VideoImage? LastFrame { get; set; }
        
        /// <summary>
        /// Up to three images to be used as style and content references.
        /// </summary>
        [JsonProperty("referenceImages", NullValueHandling = NullValueHandling.Ignore)]
        public List<VideoReferenceImage>? ReferenceImages { get; set; }
        
        /// <summary>
        /// Video to be used for video extension.
        /// </summary>
        [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
        public VideoInput? Video { get; set; }
    }

    /// <summary>
    /// Parameters for video generation.
    /// </summary>
    internal class VideoRequestParameters
    {
        /// <summary>
        /// Text describing what not to include in the video.
        /// </summary>
        [JsonProperty("negativePrompt")]
        public string? NegativePrompt { get; set; }
        
        /// <summary>
        /// The video's aspect ratio.
        /// </summary>
        [JsonProperty("aspectRatio")]
        public string? AspectRatio { get; set; }
        
        /// <summary>
        /// The video's resolution.
        /// </summary>
        [JsonProperty("resolution")]
        public string? Resolution { get; set; }
        
        /// <summary>
        /// Length of the generated video in seconds.
        /// </summary>
        [JsonProperty("durationSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? DurationSeconds { get; set; }
        
        /// <summary>
        /// Controls the generation of people.
        /// </summary>
        [JsonProperty("personGeneration")]
        public string? PersonGeneration { get; set; }
        
        /// <summary>
        /// Cloud Storage URI to store the generated videos.
        /// </summary>
        [JsonProperty("storageUri")]
        public string? StorageUri { get; set; }
        
        /// <summary>
        /// Add an invisible watermark to the generated videos.
        /// </summary>
        [JsonProperty("addWatermark")]
        public bool? AddWatermark { get; set; }
        
        /// <summary>
        /// The safety setting for generated videos.
        /// </summary>
        [JsonProperty("safetySetting")]
        public string? SafetySetting { get; set; }
    }
}