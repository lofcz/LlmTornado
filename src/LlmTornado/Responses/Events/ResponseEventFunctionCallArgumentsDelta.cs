using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when there is a partial function-call arguments delta.
/// </summary>
public class ResponseEventFunctionCallArgumentsDelta : IResponseEvent
{
    /// <summary>
    /// The type of the event. Always "response.function_call_arguments.delta".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "response.function_call_arguments.delta";

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The ID of the output item that the function-call arguments delta is added to.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item that the function-call arguments delta is added to.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The function-call arguments delta that is added.
    /// </summary>
    [JsonProperty("delta")]
    public string Delta { get; set; } = string.Empty;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public ResponseEventTypes EventType => ResponseEventTypes.ResponseFunctionCallArgumentsDelta;
} 