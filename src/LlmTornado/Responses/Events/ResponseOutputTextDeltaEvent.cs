using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when there is an additional text delta.
/// </summary>
public class ResponseOutputTextDeltaEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.output_text.delta".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.output_text.delta";

    /// <summary>
    /// The sequence number for this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The index of the content part that the text delta was added to.
    /// </summary>
    [JsonProperty("content_index")]
    public int ContentIndex { get; set; }

    /// <summary>
    /// The ID of the output item that the text delta was added to.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; }

    /// <summary>
    /// The index of the output item that the text delta was added to.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The text delta that was added.
    /// </summary>
    [JsonProperty("delta")]
    public string Delta { get; set; }

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseOutputTextDelta;
} 