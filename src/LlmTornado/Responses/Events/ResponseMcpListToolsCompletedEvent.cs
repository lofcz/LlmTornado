using Newtonsoft.Json;

namespace LlmTornado.Responses.Events
{
    /// <summary>
    /// Event emitted when the list of available MCP tools has been successfully retrieved.
    /// </summary>
    public class ResponseMcpListToolsCompletedEvent : IResponsesEvent
    {
        /// <summary>
        /// The sequence number of this event.
        /// </summary>
        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// The type of the event. Always 'response.mcp_list_tools.completed'.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "response.mcp_list_tools.completed";

        /// <summary>
        /// The type of this response event.
        /// </summary>
        [JsonIgnore]
        public ResponseEventTypes EventType => ResponseEventTypes.ResponseMcpListToolsCompleted;
    }
} 