using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses.Events;

/// <summary>
/// Event that is fired when a new content part is added.
/// </summary>
public class ResponseEventContentPartAdded : ResponseContentPartEventBase
{
    /// <summary>
    /// The type of the event. Always "response.content_part.added".
    /// </summary>
    public override string Type { get; set; } = "response.content_part.added";

    /// <summary>
    /// The type of this response event.
    /// </summary>
    [JsonIgnore]
    public override ResponseEventTypes EventType => ResponseEventTypes.ResponseContentPartAdded;
} 