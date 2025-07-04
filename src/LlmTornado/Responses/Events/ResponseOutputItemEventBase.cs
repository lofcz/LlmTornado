using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Base class for response output item events.
/// </summary>
public abstract class ResponseOutputItemEventBase : IResponsesEvent
{
    /// <summary>
    /// The type of the event.
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; set; }

    /// <summary>
    /// The sequence number of this event.
    /// </summary>
    [JsonProperty("sequence_number")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The index of the output item.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The output item that was added or completed.
    /// </summary>
    [JsonProperty("item")]
    [Newtonsoft.Json.JsonConverter(typeof(ResponseOutputItemConverter))]
    public IResponseOutputItem Item { get; set; } = null!;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public abstract ResponseEventTypes EventType { get; }
} 