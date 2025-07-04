using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a content part is done.
/// </summary>
public class ResponseContentPartDoneEvent : ResponseContentPartEventBase
{
    /// <summary>
    /// The type of the event. Always "response.content_part.done".
    /// </summary>
    public override string Type { get; set; } = "response.content_part.done";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseContentPartDone;
} 