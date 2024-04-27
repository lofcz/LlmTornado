using System;
using LlmTornado.Code;
using LlmTornado.Images;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Represents a message part
/// </summary>
public class ChatMessagePart
{
    /// <summary>
    ///     The part is a specific fragment without content.
    /// </summary>
    /// <param name="type">Type of the message part</param>
    public ChatMessagePart(ChatMessageTypes type)
    {
        Type = type;
    }
    
    /// <summary>
    ///     The part is a text fragment.
    /// </summary>
    /// <param name="text">A text fragment</param>
    public ChatMessagePart(string text)
    {
        Text = text;
        Type = ChatMessageTypes.Text;
    }

    /// <summary>
    ///     The part is an image with publicly available URL.
    /// </summary>
    /// <param name="uri">Absolute uri to the image</param>
    public ChatMessagePart(Uri uri)
    {
        Image = new ChatImage(uri.AbsoluteUri);
        Type = ChatMessageTypes.Image;
    }

    /// <summary>
    ///     The part is an image with publicly available URL.
    /// </summary>
    /// <param name="uri">Absolute uri to the image</param>
    /// <param name="imageDetail">Type of the message part</param>
    public ChatMessagePart(Uri uri, ImageDetail? imageDetail)
    {
        Image = new ChatImage(uri.AbsoluteUri, imageDetail);
        Type = ChatMessageTypes.Image;
    }

    /// <summary>
    ///     The part is an image with either publicly available URL or encoded as base64.
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="imageDetail">Type of the message part</param>
    public ChatMessagePart(string content, ImageDetail? imageDetail)
    {
        Image = new ChatImage(content, imageDetail);
        Type = ChatMessageTypes.Image;
    }

    /// <summary>
    ///     The type of message part.
    /// </summary>
    [JsonProperty("type")]
    public ChatMessageTypes Type { get; set; }

    /// <summary>
    ///     Text of the message part if type is <see cref="ChatMessageTypes.Text" />.
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }

    /// <summary>
    ///     Image of the message part if type is <see cref="ChatMessageTypes.Image" />.
    /// </summary>
    [JsonProperty("image_url")]
    public ChatImage? Image { get; set; }
}