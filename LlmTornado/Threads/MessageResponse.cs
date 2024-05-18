using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Common;
using Argon;

namespace LlmTornado.Threads;

/// <summary>
///     A message created by an Assistant or a user.
///     Messages can include text, images, and other files.
///     Messages stored as a list on the Thread.
/// </summary>
public sealed class MessageResponse : BaseResponse
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

    [JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     The thread ID that this message belongs to.
    /// </summary>
    [JsonProperty("thread_id")]
    public string ThreadId { get; private set; }

    /// <summary>
    ///     The entity that produced the message. One of user or assistant.
    /// </summary>
    [JsonProperty("role")]
    [JsonConverter(typeof(ChatMessageRole.ChatMessageRoleJsonConverter))]
    public ChatMessageRole Role { get; private set; }

    /// <summary>
    ///     The content of the message in array of text and/or images.
    /// </summary>
    [JsonProperty("content")]
    public IReadOnlyList<Content> Content { get; private set; }

    /// <summary>
    ///     If applicable, the ID of the assistant that authored this message.
    /// </summary>
    [JsonProperty("assistant_id")]
    public string AssistantId { get; private set; }

    /// <summary>
    ///     If applicable, the ID of the run associated with the authoring of this message.
    /// </summary>
    [JsonProperty("run_id")]
    public string RunId { get; private set; }

    /// <summary>
    ///     A list of file IDs that the assistant should use.
    ///     Useful for tools like 'retrieval' and 'code_interpreter' that can access files.
    ///     A maximum of 10 files can be attached to a message.
    /// </summary>
    [JsonProperty("file_ids")]
    public IReadOnlyList<string> FileIds { get; private set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; private set; }

    public static implicit operator string(MessageResponse message)
    {
        return message?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }

    /// <summary>
    ///     Formats all of the <see cref="Content" /> items into a single string,
    ///     putting each item on a new line.
    /// </summary>
    /// <returns><see cref="string" /> of all <see cref="Content" />.</returns>
    public string PrintContent()
    {
        return string.Join("\n", Content.Select(content => content?.ToString()));
    }
}