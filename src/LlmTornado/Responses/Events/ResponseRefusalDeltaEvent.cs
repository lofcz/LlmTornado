using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when there is a partial refusal text.
/// </summary>
public class ResponseRefusalDeltaEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.refusal.delta".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.refusal.delta";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The index of the content part that the refusal text is added to.
    /// </summary>
    [JsonProperty("content_index")]
    public int ContentIndex { get; set; }

    /// <summary>
    /// The ID of the output item that the refusal text is added to.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item that the refusal text is added to.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The refusal text that is added.
    /// </summary>
    [JsonProperty("delta")]
    public string Delta { get; set; } = string.Empty;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseRefusalDelta;
} 