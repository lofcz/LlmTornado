using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when an MCP tool call has completed successfully.
/// </summary>
public class ResponseMcpCallCompletedEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.mcp_call.completed".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.mcp_call.completed";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseMcpCallCompleted;
} 