using Newtonsoft.Json;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when a code interpreter call is in progress.
    /// </summary>
    public class ResponseEventCodeInterpreterCallInProgress : IResponseEvent
    {
        /// <summary>
        /// The unique identifier of the code interpreter tool call item.
        /// </summary>
        [JsonProperty("item_id")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// The index of the output item in the response for which the code interpreter call is in progress.
        /// </summary>
        [JsonProperty("output_index")]
        public int OutputIndex { get; set; }

        /// <summary>
        /// The sequence number of this event, used to order streaming events.
        /// </summary>
        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// The type of the event. Always 'response.code_interpreter_call.in_progress'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.code_interpreter_call.in_progress";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseCodeInterpreterCallInProgress;
    }
} 