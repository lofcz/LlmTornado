using LlmTornado.Chat;
using LlmTornado.Images;
using LlmTornado.Videos;
using Newtonsoft.Json;

namespace LlmTornado.Batch;

/// <summary>
/// Specifies the target API endpoint for a batch request item.
/// </summary>
public enum BatchRequestEndpoint
{
    /// <summary>
    /// The /v1/chat/completions endpoint.
    /// </summary>
    ChatCompletions,
    
    /// <summary>
    /// The /v1/images/generations endpoint.
    /// </summary>
    ImageGenerations,
    
    /// <summary>
    /// The /v1/images/edits endpoint.
    /// </summary>
    ImageEdits,

    /// <summary>
    /// The /v1/videos endpoint.
    /// </summary>
    Videos
}

/// <summary>
/// An individual request in a batch.
/// </summary>
public class BatchRequestItem
{
    /// <summary>
    /// Developer-provided ID for matching results to requests.
    /// Must be unique for each request within the batch.
    /// </summary>
    [JsonProperty("custom_id")]
    public string CustomId { get; set; } = string.Empty;
    
    /// <summary>
    /// The chat request parameters for this batch item.
    /// Used when <see cref="Endpoint"/> is <see cref="BatchRequestEndpoint.ChatCompletions"/>.
    /// </summary>
    [JsonProperty("params")]
    public ChatRequest? Params { get; set; }
    
    /// <summary>
    /// The image generation request parameters for this batch item.
    /// Used when <see cref="Endpoint"/> is <see cref="BatchRequestEndpoint.ImageGenerations"/>.
    /// </summary>
    [JsonIgnore]
    public ImageGenerationRequest? ImageGenerationParams { get; set; }
    
    /// <summary>
    /// The image edit request parameters for this batch item.
    /// Used when <see cref="Endpoint"/> is <see cref="BatchRequestEndpoint.ImageEdits"/>.
    /// </summary>
    [JsonIgnore]
    public ImageEditRequest? ImageEditParams { get; set; }

    /// <summary>
    /// The video generation request parameters for this batch item.
    /// Used when <see cref="Endpoint"/> is <see cref="BatchRequestEndpoint.Videos"/>.
    /// </summary>
    [JsonIgnore]
    public VideoGenerationRequest? VideoGenerationParams { get; set; }
    
    /// <summary>
    /// The target API endpoint for this batch request item.
    /// Defaults to <see cref="BatchRequestEndpoint.ChatCompletions"/>.
    /// </summary>
    [JsonIgnore]
    public BatchRequestEndpoint Endpoint { get; set; } = BatchRequestEndpoint.ChatCompletions;
    
    /// <summary>
    /// Creates an empty batch request item.
    /// </summary>
    public BatchRequestItem()
    {
    }
    
    /// <summary>
    /// Creates a batch request item with the specified custom ID and chat request parameters.
    /// </summary>
    /// <param name="customId">Developer-provided ID for matching results</param>
    /// <param name="chatRequest">The chat request parameters</param>
    public BatchRequestItem(string customId, ChatRequest chatRequest)
    {
        CustomId = customId;
        Params = chatRequest;
        Endpoint = BatchRequestEndpoint.ChatCompletions;
    }
    
    /// <summary>
    /// Creates a batch request item with the specified custom ID and image generation parameters.
    /// </summary>
    /// <param name="customId">Developer-provided ID for matching results</param>
    /// <param name="imageGenerationRequest">The image generation request parameters</param>
    public BatchRequestItem(string customId, ImageGenerationRequest imageGenerationRequest)
    {
        CustomId = customId;
        ImageGenerationParams = imageGenerationRequest;
        Endpoint = BatchRequestEndpoint.ImageGenerations;
    }
    
    /// <summary>
    /// Creates a batch request item with the specified custom ID and image edit parameters.
    /// </summary>
    /// <param name="customId">Developer-provided ID for matching results</param>
    /// <param name="imageEditRequest">The image edit request parameters</param>
    public BatchRequestItem(string customId, ImageEditRequest imageEditRequest)
    {
        CustomId = customId;
        ImageEditParams = imageEditRequest;
        Endpoint = BatchRequestEndpoint.ImageEdits;
    }

    /// <summary>
    /// Creates a batch request item with the specified custom ID and video generation parameters.
    /// </summary>
    /// <param name="customId">Developer-provided ID for matching results</param>
    /// <param name="videoGenerationRequest">The video generation request parameters</param>
    public BatchRequestItem(string customId, VideoGenerationRequest videoGenerationRequest)
    {
        CustomId = customId;
        VideoGenerationParams = videoGenerationRequest;
        Endpoint = BatchRequestEndpoint.Videos;
    }
}
