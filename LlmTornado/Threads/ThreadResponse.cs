using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     A conversation session between an Assistant and a user.
///     Threads store Messages and automatically handle truncation to fit content into a model's context.
/// </summary>
public sealed class ThreadResponse
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; private set; }

    /// <summary>
    ///     The object type, which is always thread.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; private set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the thread was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; private set; }

    [JsonIgnore] public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     Set of up to 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    public static implicit operator string(ThreadResponse? thread)
    {
        return thread?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }
}