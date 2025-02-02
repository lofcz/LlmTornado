using System.Collections.Generic;
using LlmTornado.Chat;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
///     A message created by an Assistant or a user.
///     Messages can include text, images, and other files.
///     Messages stored as a list on the Thread.
/// </summary>
public sealed class Message : ApiResultBase
{
    /// <summary>
    ///     The identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    /// <summary>
    ///     The Unix timestamp (in seconds) for when the assistant was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAt
    {
        get => CreatedUnixTime ?? 0;
        set => CreatedUnixTime = value;
    }
    
    /// <summary>
    /// The Unix timestamp (in seconds) for when the message was completed.
    /// </summary>
    [JsonProperty("completed_at")]
    public long? CompletedAt { get; set; }
    
    /// <summary>
    /// The Unix timestamp (in seconds) for when the message was marked as incomplete.
    /// </summary>
    [JsonProperty("incomplete_at")]
    public long? IncompleteAt { get; set; }

    /// <summary>
    /// On an incomplete message, details about why the message is incomplete.
    /// </summary>
    [JsonProperty("incomplete_details")]
    public MessageIncompleteDetails? IncompleteDetails { get; set; }
    
    /// <summary>
    ///     The thread ID that this message belongs to.
    /// </summary>
    [JsonProperty("thread_id")]
    public string ThreadId { get; set; } = null!;
    
    /// <summary>
    ///     The status of the message, which can be either in_progress, incomplete, or completed.
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = null!;

    /// <summary>
    ///     The entity that produced the message. One of user or assistant.
    /// </summary>
    [JsonProperty("role")]
    [JsonConverter(typeof(ChatMessageRole.ChatMessageRoleJsonConverter))]
    public ChatMessageRole Role { get; private set; } = null!;

    /// <summary>
    ///     The content of the message in array of text and/or images.
    /// </summary>
    [JsonProperty("content")]
    public IReadOnlyList<MessageContent> Content { get; private set; } = null!;

    /// <summary>
    ///     If applicable, the ID of the assistant that authored this message.
    /// </summary>
    [JsonProperty("assistant_id")]
    public string? AssistantId { get; set; }

    /// <summary>
    ///     The ID of the run associated with the creation of this message.
    ///     Value is null when messages are created manually using the create message or create thread endpoints.
    /// </summary>
    [JsonProperty("run_id")]
    public string? RunId { get; set; }
    
    /// <summary>
    ///     A list of files attached to the message, and the tools they were added to.
    /// </summary>
    [JsonProperty("attachments")]
    public IReadOnlyList<MessageAttachment>? Attachments { get; set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = null!;
}