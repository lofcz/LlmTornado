using System;
using Newtonsoft.Json;

namespace OpenAiNg.Common;

public sealed class EventResponse
{
    public EventResponse()
    {
    }

#pragma warning disable CS0618 // Type or member is obsolete
    internal EventResponse(Event @event)
    {
        Object = @event.Object;
        CreatedAtUnixTimeSeconds = @event.CreatedAtUnixTimeSeconds;
        Level = @event.Level;
        Message = @event.Message;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [JsonProperty("object")] public string Object { get; private set; }

    [JsonProperty("created_at")] public int CreatedAtUnixTimeSeconds { get; private set; }

    [JsonIgnore] public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    [JsonProperty("level")] public string Level { get; private set; }

    [JsonProperty("message")] public string Message { get; private set; }
}