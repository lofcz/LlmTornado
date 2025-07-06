using Newtonsoft.Json;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when the system is in the process of retrieving the list of available MCP tools.
    /// </summary>
    public class ResponseEventMcpListToolsInProgress : IResponseEvent
    {
        /// <summary>
        /// The sequence number of this event.
        /// </summary>
        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// The type of the event. Always 'response.mcp_list_tools.in_progress'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.mcp_list_tools.in_progress";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseMcpListToolsInProgress;
    }
} 