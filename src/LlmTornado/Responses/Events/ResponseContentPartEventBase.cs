using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Base class for response content part events.
/// </summary>
public abstract class ResponseContentPartEventBase : IResponsesEvent
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
    /// The index of the content part.
    /// </summary>
    [JsonProperty("content_index")]
    public int ContentIndex { get; set; }

    /// <summary>
    /// The ID of the output item that the content part was added to.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the output item that the content part was added to.
    /// </summary>
    [JsonProperty("output_index")]
    public int OutputIndex { get; set; }

    /// <summary>
    /// The content part that was added.
    /// </summary>
    [JsonProperty("part")]
    [JsonConverter(typeof(ResponseContentPartConverter))]
    public IResponseContentPart Part { get; set; } = null!;

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public abstract ResponseEventTypes EventType { get; }
} 