using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a web search call is executing.
/// </summary>
public class ResponseEventWebSearchCallSearching : IResponseEvent
{
    /// <summary>
    /// The type of the event. Always "response.web_search_call.searching".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.web_search_call.searching";

    /// <summary>
    /// The sequence number of the web search call being processed.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Unique ID for the output item associated with the web search call.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item that the web search call is associated with.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseWebSearchCallSearching;
} 