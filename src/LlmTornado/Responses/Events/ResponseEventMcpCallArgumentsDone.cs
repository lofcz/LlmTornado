using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when the arguments for an MCP tool call are finalized.
/// </summary>
public class ResponseEventMcpCallArgumentsDone : IResponseEvent
{
    /// <summary>
    /// The type of the event. Always "response.mcp_call.arguments.done".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.mcp_call.arguments.done";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The unique identifier of the MCP tool call item being processed.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item in the response's output array.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The finalized arguments for the MCP tool call.
    /// </summary>
    [JsonProperty("arguments")]
    public JObject Arguments { get; set; } = new JObject();

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseMcpCallArgumentsDone;
} 