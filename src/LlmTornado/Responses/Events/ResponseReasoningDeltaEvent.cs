using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when there is a delta (partial update) to the reasoning content.
    /// </summary>
    public class ResponseReasoningDeltaEvent : IResponsesEvent
    {
        /// <summary>
        /// The index of the reasoning content part within the output item.
        /// </summary>
        [JsonProperty("content_index")]
        public int ContentIndex { get; set; }

        /// <summary>
        /// The partial update to the reasoning content.
        /// </summary>
        [JsonProperty("delta")]
        public JObject Delta { get; set; } = new JObject();

        /// <summary>
        /// The unique identifier of the item for which reasoning is being updated.
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
        /// The type of the event. Always 'response.reasoning.delta'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.reasoning.delta";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseReasoningDelta;
    }
} 