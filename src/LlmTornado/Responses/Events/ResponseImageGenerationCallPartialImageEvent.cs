using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a partial image is available during image generation streaming.
/// </summary>
public class ResponseImageGenerationCallPartialImageEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.image_generation_call.partial_image".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.image_generation_call.partial_image";

    /// <summary>
    /// The sequence number of the image generation item being processed.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The unique identifier of the image generation item being processed.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response's output array.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// Base64-encoded partial image data, suitable for rendering as an image.
    /// </summary>
    [JsonProperty("partial_image_b64")]
    public string PartialImageB64 { get; set; } = string.Empty;

    /// <summary>
    /// 0-based index for the partial image (backend is 1-based, but this is 0-based for the user).
    /// </summary>
    [JsonProperty("partial_image_index")]
    public int PartialImageIndex { get; set; }

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseImageGenerationCallPartialImage;
} 