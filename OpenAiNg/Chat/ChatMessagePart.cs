using System;
using Newtonsoft.Json;
using OpenAiNg.Code;

namespace OpenAiNg.Chat;

/// <summary>
///     Represents a message part
/// </summary>
public class ChatMessagePart
{
    /// <summary>
    /// The part is a text fragment
    /// </summary>
    /// <param name="text">A text fragment</param>
    public ChatMessagePart(string text)
    {
        Text = text;
        Type = ChatMessageTypes.Text;
    }

    /// <summary>
    /// The part is an image
    /// </summary>
    /// <param name="uri">Absolute uri to the image</param>
    public ChatMessagePart(Uri uri)
    {
        Image = new ChatImage(uri.AbsoluteUri);
        Type = ChatMessageTypes.Image;
    }
    
    /// <summary>
    /// The type of message part
    /// </summary>
    [JsonProperty("type")]
    public ChatMessageTypes Type { get; set; }
    
    /// <summary>
    /// Text of the message part if type is <see cref="ChatMessageTypes.Text"/>
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }
    
    /// <summary>
    /// Image of the message part if type is <see cref="ChatMessageTypes.Image"/>
    /// </summary>
    [JsonProperty("image_url")]
    public ChatImage? Image { get; set; }
}