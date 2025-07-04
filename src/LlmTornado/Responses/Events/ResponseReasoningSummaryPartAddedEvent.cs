using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a new reasoning summary part is added.
/// </summary>
public class ResponseReasoningSummaryPartAddedEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.reasoning_summary_part.added".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.reasoning_summary_part.added";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The ID of the item this summary part is associated with.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item this summary part is associated with.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The index of the summary part within the reasoning summary.
    /// </summary>
    [JsonProperty("summary_index")]
    public int SummaryIndex { get; set; }

    /// <summary>
    /// The summary part that was added.
    /// </summary>
    [JsonProperty("part")]
    public ReasoningSummaryPart Part { get; set; } = new();

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseReasoningSummaryPartAdded;
}

/// <summary>
/// Represents a reasoning summary part.
/// </summary>
public class ReasoningSummaryPart
{
    /// <summary>
    /// The type of the summary part. Always "summary_text".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "summary_text";

    /// <summary>
    /// The text of the summary part.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
} 