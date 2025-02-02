using LlmTornado.Common;
using Newtonsoft.Json;

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
    public required string Type { get; set; }
}

/// <summary>
/// Represents a text-based message content type within the messaging system.
/// </summary>
public sealed class MessageContentText : MessageContent
{
    /// <inheritdoc />
    public MessageContentText()
    {
        Type = "text";
    }

    /// <summary>
    /// 
    /// </summary>
    public MessageContentTextData? MessageContentTextData { get; set; }
}

/// <summary>
/// Represents a message content that contains an image file.
/// </summary>
public sealed class MessageContentImageFile : MessageContent
{
    /// <inheritdoc />
    public MessageContentImageFile()
    {
        Type = "image_file";
    }
    
    
    /// <summary>
    ///     Object that represents ImageFile
    /// </summary>
    [JsonProperty("image_file")]
    public ImageFile? ImageFile { get; set; }
}

/// <summary>
/// Represents a type of message content that contains an image URL.
/// </summary>
public sealed class MessageContentImageUrl : MessageContent
{
    /// <inheritdoc />
    public MessageContentImageUrl()
    {
        Type = "image_url"; //OpenAI api: The type of the content part. -- not sure what to put in here
    }

    /// <summary>
    ///     Object that represents ImageUrl
    /// </summary>
    public ImageUrl? ImageUrl { get; set; }
}

/// <summary>
/// Represents a specific type of message content that indicates a refusal.
/// </summary>
public sealed class MessageContentRefusal : MessageContent
{
    /// <inheritdoc />
    public MessageContentRefusal()
    {
        Type = "refusal";
    }
    
    /// <summary>
    ///     Refusal reason
    /// </summary>
    [JsonProperty("refusal")]
    public string? Refusal { get; set; }
}
