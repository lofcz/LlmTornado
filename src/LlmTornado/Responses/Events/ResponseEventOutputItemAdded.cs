using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when an output item is added to the response.
/// </summary>
public class ResponseEventOutputItemAdded : ResponseOutputItemEventBase
{
    /// <summary>
    /// The type of the event. Always "response.output_item.added".
    /// </summary>
    public override string Type { get; set; } = "response.output_item.added";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseOutputItemAdded;
} 