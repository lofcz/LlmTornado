using System;
using System.Collections.Generic;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Represents a conversation.
/// </summary>
public class ConversationResult
{
    /// <summary>
    /// The unique identifier for the conversation.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type, which is always "conversation".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "conversation";

    /// <summary>
    /// The Unix timestamp (in seconds) of when the conversation was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int CreatedUnixTime { get; set; }

    /// <summary>
    /// The date time of when the conversation was created.
    /// </summary>
    [JsonIgnore] 
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedUnixTime).DateTime;
    
    /// <summary>
    /// A set of key-value pairs that can be attached to the conversation.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
