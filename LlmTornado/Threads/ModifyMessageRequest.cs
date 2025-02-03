using System.Collections.Generic;
using LlmTornado.Chat;
using Newtonsoft.Json;

namespace LlmTornado.Threads;
/// <summary>
/// Request to modify a message object
/// Based on <a href="https://platform.openai.com/docs/api-reference/messages/modifyMessage">OpenAI API Reference - Modify Message</a>
/// </summary>
public sealed class ModifyMessageRequest
{
    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}