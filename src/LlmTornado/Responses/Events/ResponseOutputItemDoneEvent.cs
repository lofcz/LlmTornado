using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when an output item is marked done.
/// </summary>
public class ResponseOutputItemDoneEvent : ResponseOutputItemEventBase
{
    /// <summary>
    /// The type of the event. Always "response.output_item.done".
    /// </summary>
    public override string Type { get; set; } = "response.output_item.done";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseOutputItemDone;
} 