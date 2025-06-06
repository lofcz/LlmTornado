using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a request to create a message in the context of a chat thread.
/// Based on <a href="https://platform.openai.com/docs/api-reference/messages/createMessage">OpenAI API Reference - Create Message</a>
/// </summary>
public sealed class CreateMessageRequest
{
    /// <summary>
    ///     Constructor.
    /// </summary>
    public CreateMessageRequest()
    {
        Role = ChatMessageRoles.User;
        Content ??= [];
    }

    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(string messageContent) : this()
    {
        Content = 
        [
            new MessageContentTextRequest
            {
                Text = messageContent
            }
        ];
    }
    
    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(IEnumerable<MessageContentTextRequest> messageContent) : this()
    {
        Content = messageContent.ToList();
    }
    
    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(IEnumerable<MessageContent> messageContent) : this()
    {
        Content = messageContent.ToList();
    }
    
    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(string messageContent, ChatMessageRoles role)
    {
        Content = 
        [
            new MessageContentTextRequest
            {
                Text = messageContent
            }
        ];

        Role = role;
    }
    
    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(IEnumerable<MessageContentTextRequest> messageContent, ChatMessageRoles role)
    {
        Content = messageContent.ToList();
        Role = role;
    }
    
    /// <summary>
    /// Represents a request to create a message in the context of a chat thread.
    /// </summary>
    public CreateMessageRequest(List<MessageContentTextRequest> messageContent, ChatMessageRoles role)
    {
        Content = messageContent.ToList();
        Role = role;
    }

    /// <summary>
    ///     The role of the entity that is creating the message.
    /// </summary>
    /// <remarks>
    ///     Currently only user and assistant is supported.
    /// </remarks>
    [JsonProperty("role")]
    public ChatMessageRoles Role { get; set; }

    /// <summary>
    ///     The content of the message.
    /// </summary>
    [JsonProperty("content")]
    [JsonConverter(typeof(MessageContentJsonConverter))]
    public IReadOnlyList<MessageContent> Content { get; set; }

    /// <summary>
    ///     A list of File IDs that the message should use. There can be a maximum of 10 files attached to a message.
    ///     Useful for tools like retrieval and code_interpreter that can access and use files.
    /// </summary>
    [JsonProperty("attachments")]
    public IReadOnlyList<MessageAttachment>? Attachments { get; set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}