using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Threads;

/// <summary>
/// Base abstract class representing the content of a message in a chat thread.
/// Acts as a generic representation for various types of message content.
/// Derived classes should implement specific types of message content.
/// </summary>
public abstract class MessageContent
{
    /// <summary>
    /// Type of the message content.
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public MessageContentTypes Type { get; set; }
}

/// <summary>
/// Enum defining the various types of message content that can exist within a chat thread.
/// Each value represents a specific kind of content, such as text, images (by file or URL), or system-generated refusals.
/// Used to identify and manage the content type in a consistent manner.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MessageContentTypes
{
    /// <summary>
    /// Represents a text-based content type for a message in the messaging system.
    /// Used to identify messages containing plain text content.
    /// </summary>
    [EnumMember(Value = "text")]
    Text,

    /// <summary>
    /// Represents a message content type that contains an image file.
    /// </summary>
    [EnumMember(Value = "image_file")]
    ImageFile,

    /// <summary>
    /// Represents the message content type for image URLs.
    /// This enum member is used to indicate that the content of the message
    /// is an image that is referred to by a URL.
    /// </summary>
    [EnumMember(Value = "image_url")]
    ImageUrl,

    /// <summary>
    /// Represents a refusal message content type in a messaging system.
    /// Used to denote messages that indicate a refusal or denial within the context of the chat thread.
    /// </summary>
    [EnumMember(Value = "refusal")]
    Refusal
}

/// <summary>
/// Represents a text-based message content type within the messaging system.
/// </summary>
public sealed class MessageContentTextResponse : MessageContent
{
    /// <inheritdoc />
    internal MessageContentTextResponse()
    {
        Type = MessageContentTypes.Text;
    }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("text")]
    public MessageContentTextData? MessageContentTextData { get; set; }
    
    /// <summary>
    /// Only when part of delta object
    /// </summary>
    [JsonProperty("index")]
    public int? Index { get; set; }
}

/// <summary>
/// Represents a request for text-based message content in the messaging system.
/// This class is used to specify and serialize the necessary data for creating or modifying a text message content.
/// </summary>
public sealed class MessageContentTextRequest : MessageContent
{
    /// <inheritdoc />
    public MessageContentTextRequest()
    {
        Type = MessageContentTypes.Text;
    }

    /// <summary>
    ///     Text content to be sent to the model
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = null!;
}

/// <summary>
/// Represents a message content that contains an image file.
/// </summary>
public sealed class MessageContentImageFile : MessageContent
{
    /// <inheritdoc />
    public MessageContentImageFile()
    {
        Type = MessageContentTypes.ImageFile;
    }
    
    /// <summary>
    ///     Object that represents ImageFile
    /// </summary>
    [JsonProperty("image_file")]
    public ImageFile? ImageFile { get; set; }
    
    /// <summary>
    ///     Text content to be sent to the model
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = null!;
}

/// <summary>
/// Represents a type of message content that contains an image URL.
/// </summary>
public sealed class MessageContentImageUrl : MessageContent
{
    /// <inheritdoc />
    public MessageContentImageUrl()
    {
        Type = MessageContentTypes.ImageUrl;
    }

    /// <summary>
    ///     Object that represents ImageUrl
    /// </summary>
    public ImageUrl? ImageUrl { get; set; }
    
    /// <summary>
    ///     Text content to be sent to the model
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = null!;
}

/// <summary>
/// Represents a specific type of message content that indicates a refusal.
/// </summary>
public sealed class MessageContentRefusal : MessageContent
{
    /// <inheritdoc />
    public MessageContentRefusal()
    {
        Type = MessageContentTypes.Refusal;
    }
    
    /// <summary>
    ///     Refusal reason
    /// </summary>
    [JsonProperty("refusal")]
    public string? Refusal { get; set; }
    
    /// <summary>
    ///     Text content to be sent to the model
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = null!;
}

internal class MessageContentJsonConverter : JsonConverter<IReadOnlyList<MessageContent>>
{
    public override void WriteJson(JsonWriter writer, IReadOnlyList<MessageContent>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override IReadOnlyList<MessageContent>? ReadJson(JsonReader reader, Type objectType, IReadOnlyList<MessageContent>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        List<MessageContent> messageContents = [];
        JArray array = JArray.Load(reader);
        
        foreach (JToken jToken in array)
        {
            if (jToken is JObject jObject)
            {
                MessageContentTypes? messageContentTypeEnum = jObject["type"]?.ToObject<MessageContentTypes>();
                    
                MessageContent? messageContent = messageContentTypeEnum switch
                {
                    MessageContentTypes.Text => jObject.ToObject<MessageContentTextResponse>(serializer),
                    MessageContentTypes.ImageFile => jObject.ToObject<MessageContentImageFile>(serializer),
                    MessageContentTypes.ImageUrl => jObject.ToObject<MessageContentImageUrl>(serializer),
                    MessageContentTypes.Refusal => jObject.ToObject<MessageContentRefusal>(serializer),
                    _ => null
                };

                if (messageContent is not null)
                {
                    messageContents.Add(messageContent);
                }
            }
        }
        
        return messageContents;
    }
}
