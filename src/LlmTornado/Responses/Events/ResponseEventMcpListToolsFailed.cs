using Newtonsoft.Json;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when the attempt to list available MCP tools has failed.
    /// </summary>
    public class ResponseEventMcpListToolsFailed : IResponseEvent
    {
        /// <summary>
        /// The sequence number of this event.
        /// </summary>
        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// The type of the event. Always 'response.mcp_list_tools.failed'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.mcp_list_tools.failed";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseMcpListToolsFailed;
    }
} 