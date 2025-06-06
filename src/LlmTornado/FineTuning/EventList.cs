using System;
using System.Collections.Generic;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

[Obsolete("Use ListResponse<EventResponse>")]
public sealed class EventList
{
    [JsonProperty("object")] 
    public string Object { get; private set; }

    [JsonProperty("data")] 
    public IReadOnlyList<Event> Events { get; private set; }
    
    [JsonProperty("has_more")]
    public bool HasMore { get; private set; }
}