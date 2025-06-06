using System;
using Newtonsoft.Json;

namespace LlmTornado.Common;

[Obsolete("use EventResponse")]
public sealed class Event
{
    [JsonProperty("object")] 
    public string Object { get; private set; }
    
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; private set; }

    [Obsolete("use CreatedAtUnixTimeSeconds")]
    public int CreatedAtUnixTime => CreatedAtUnixTimeSeconds;

    [JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    [JsonProperty("level")] 
    public string Level { get; private set; }

    [JsonProperty("message")]
    public string Message { get; private set; }

    public static implicit operator EventResponse(Event @event)
    {
        return new EventResponse(@event);
    }
}