using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when an image generation tool call has completed and the final image is available.
/// </summary>
public class ResponseImageGenerationCallCompletedEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.image_generation_call.completed".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.image_generation_call.completed";

    /// <summary>
    /// The sequence number of this event.
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
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseImageGenerationCallCompleted;
} 