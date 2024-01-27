// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Common;

[Obsolete("use EventResponse")]
public sealed class Event
{
    [JsonInclude]
    [JsonProperty("object")]
    public string Object { get; private set; }

    [JsonInclude]
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; private set; }

    [Obsolete("use CreatedAtUnixTimeSeconds")]
    public int CreatedAtUnixTime => CreatedAtUnixTimeSeconds;

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    [JsonInclude]
    [JsonProperty("level")]
    public string Level { get; private set; }

    [JsonInclude]
    [JsonProperty("message")]
    public string Message { get; private set; }

    public static implicit operator EventResponse(Event @event) => new EventResponse(@event);
}

