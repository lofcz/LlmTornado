using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when function-call arguments are finalized.
/// </summary>
public class ResponseFunctionCallArgumentsDoneEvent : IResponsesEvent
{
    /// <summary>
    /// The type of the event. Always "response.function_call_arguments.done".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.function_call_arguments.done";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The ID of the item.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The function-call arguments.
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseFunctionCallArgumentsDone;
} 