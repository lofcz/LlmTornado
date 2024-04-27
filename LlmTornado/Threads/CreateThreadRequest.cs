using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

public sealed class CreateThreadRequest
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="messages">
    ///     A list of messages to start the thread with.
    /// </param>
    /// <param name="metadata">
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </param>
    public CreateThreadRequest(IEnumerable<Message> messages = null, IReadOnlyDictionary<string, string> metadata = null)
    {
        Messages = messages?.ToList();
        Metadata = metadata;
    }

    /// <summary>
    ///     A list of messages to start the thread with.
    /// </summary>
    [JsonProperty("messages")]
    public IReadOnlyList<Message> Messages { get; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public static implicit operator CreateThreadRequest(string message)
    {
        return new CreateThreadRequest(new[] { new Message(message) });
    }
}