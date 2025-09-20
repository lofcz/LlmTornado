using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// The request body for creating a conversation.
/// </summary>
public class ResponsesConversationRequest
{
    /// <summary>
    /// Initial items to include in the conversation context.
    /// You may add up to 20 items at a time.
    /// </summary>
    [JsonProperty("items")]
    public List<IResponsesConversationItem>? Items { get; set; }

    /// <summary>
    /// Set of 16 key-value pairs that can be attached to an object. This can be
    /// useful for storing additional information about the object in a structured
    /// format, and querying for objects via API or the dashboard.
    /// Keys are strings with a maximum length of 64 characters. Values are strings
    /// with a maximum length of 512 characters.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
