using Newtonsoft.Json;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when the reasoning summary content is finalized for an item.
    /// </summary>
    public class ResponseEventReasoningSummaryDone : IResponseEvent
    {
        /// <summary>
        /// The unique identifier of the item for which the reasoning summary is finalized.
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
        /// The index of the summary part within the output item.
        /// </summary>
        [JsonProperty("summary_index")]
        public int SummaryIndex { get; set; }

        /// <summary>
        /// The finalized reasoning summary text.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The type of the event. Always 'response.reasoning_summary.done'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.reasoning_summary.done";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseReasoningSummaryDone;
    }
} 