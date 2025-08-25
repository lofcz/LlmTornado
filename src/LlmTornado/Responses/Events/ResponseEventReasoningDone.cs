using Newtonsoft.Json;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event emitted when the reasoning content is finalized for an item.
/// </summary>
public class ResponseEventReasoningDone : IResponseEvent
{
    /// <summary>
    /// The index of the reasoning content part within the output item.
    /// </summary>
    [JsonProperty("content_index")]
    public int ContentIndex { get; set; }

    /// <summary>
    /// The unique identifier of the item for which reasoning is finalized.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response's output array.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The finalized reasoning text.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The type of the event. Always 'response.reasoning.done'.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.reasoning.done";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseReasoningDone;
}