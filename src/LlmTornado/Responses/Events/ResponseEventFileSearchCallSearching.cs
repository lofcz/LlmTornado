using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a file search is currently searching.
/// </summary>
public class ResponseEventFileSearchCallSearching : IResponseEvent
{
    /// <summary>
    /// The type of the event. Always "response.file_search_call.searching".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.file_search_call.searching";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The ID of the output item that the file search call is initiated.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item that the file search call is searching.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseFileSearchCallSearching;
} 