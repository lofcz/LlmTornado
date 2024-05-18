using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlmTornado.Common;
using Argon;

namespace LlmTornado.FineTuning;

[Obsolete("Use ListResponse<EventResponse>")]
public sealed class EventList
{
    [JsonInclude] [JsonProperty("object")] public string Object { get; private set; }

    [JsonInclude] [JsonProperty("data")] public IReadOnlyList<Event> Events { get; private set; }

    [JsonInclude]
    [JsonProperty("has_more")]
    public bool HasMore { get; private set; }
}