using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Chat.Vendors.Anthropic;
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
    ///     Part with unset content. When using empty ctor you are responsible for providing the correct type before sending the request.
    /// </summary>
    public ChatMessagePart()
    {
        
    }
    
    /// <summary>
    ///     The part is a specific fragment without content.
    /// </summary>
    /// <param name="type">Type of the message part</param>
    public ChatMessagePart(ChatMessageTypes type)
    {
        Type = type;
    }
    
    /// <summary>
    /// Sets the part to <see cref="ChatMessageTypes.FileLink"/>. Supported only by Google.
    /// </summary>
    /// <param name="fileLinkData"></param>
    public ChatMessagePart(ChatMessagePartFileLinkData fileLinkData)
    {
        Type = ChatMessageTypes.FileLink;
        FileLinkData = fileLinkData;
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
    
    public ChatMessagePart(string text, IChatMessagePartVendorExtensions vendorExtensions)
    {
        Text = text;
        Type = ChatMessageTypes.Text;
        VendorExtensions = vendorExtensions;
    }
    
    /// <summary>
    ///     The part is an audio fragment.
    /// </summary>
    /// <param name="base64EncodedAudio">Audio data</param>
    /// <param name="format">Audio format</param>
    public ChatMessagePart(string base64EncodedAudio, ChatAudioFormats format)
    {
        Type = ChatMessageTypes.Audio;
        Audio = new ChatAudio(base64EncodedAudio, format);
    }
    
    /// <summary>
    ///     The part is an audio fragment.
    /// </summary>
    /// <param name="audioBytes">Audio data</param>
    /// <param name="format">Audio format</param>
    public ChatMessagePart(byte[] audioBytes, ChatAudioFormats format)
    {
        Type = ChatMessageTypes.Audio;
        Audio = new ChatAudio(Convert.ToBase64String(audioBytes), format);
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
    /// <param name="imageDetail">Image settings</param>
    public ChatMessagePart(Uri uri, ImageDetail imageDetail = ImageDetail.Auto)
    {
        Image = new ChatImage(uri.AbsoluteUri, imageDetail);
        Type = ChatMessageTypes.Image;
    }

    /// <summary>
    ///     The part is an image with either publicly available URL or encoded as base64.
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="imageDetail">Image settings</param>
    public ChatMessagePart(string content, ImageDetail imageDetail)
    {
        Image = new ChatImage(content, imageDetail);
        Type = ChatMessageTypes.Image;
    }
    
    /// <summary>
    ///     The part is an image with either publicly available URL or encoded as base64.
    /// </summary>
    /// <param name="content">Publicly available URL to the image or base64 encoded content</param>
    /// <param name="imageDetail">Image settings</param>
    /// <param name="mimeType">MIME type of the image</param>
    public ChatMessagePart(string content, ImageDetail imageDetail, string? mimeType)
    {
        Image = new ChatImage(content, imageDetail)
        {
            MimeType = mimeType
        };
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
    
    /// <summary>
    ///     Audio of the message part if type is <see cref="ChatMessageTypes.Audio" />.
    /// </summary>
    [JsonProperty("input_audio")]
    public ChatAudio? Audio { get; set; }
    
    /// <summary>
    ///     Specific features supported only by certain providers
    /// </summary>
    [JsonIgnore]
    public IChatMessagePartVendorExtensions? VendorExtensions { get; set; }
    
    /// <summary>
    /// File link data.
    /// </summary>
    [JsonIgnore]
    public ChatMessagePartFileLinkData? FileLinkData { get; set; }

    /// <summary>
    /// This can be set to anything and is ignored by the library. Can be used for storing data for rich rendering.
    /// </summary>
    [JsonIgnore]
    public object? CustomData { get; set; }
    
    /// <summary>
    ///     Creates an audio part from a given stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static async Task<ChatMessagePart> Create(Stream stream, ChatAudioFormats format)
    {
        using MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        byte[] data = ms.ToArray();

        return new ChatMessagePart(data, format);
    }
    
    /// <summary>
    ///     Creates an audio part from a given byte array.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static ChatMessagePart Create(byte[] data, ChatAudioFormats format)
    {
        return new ChatMessagePart(data, format);
    }
    
    /// <summary>
    ///     Creates an audio part from a given byte enumerable.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static ChatMessagePart Create(IEnumerable<byte> data, ChatAudioFormats format)
    {
        return new ChatMessagePart(data.ToArray(), format);
    }
    
    /// <summary>
    ///     Creates an audio part from a given byte enumerable.
    /// </summary>
    /// <param name="base64EncodedAudio"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static ChatMessagePart Create(string base64EncodedAudio, ChatAudioFormats format)
    {
        return new ChatMessagePart(base64EncodedAudio, format);
    }
    
    
}