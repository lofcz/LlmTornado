using System;
using System.IO;
using Newtonsoft.Json;

namespace LlmTornado.Audio;

/// <summary>
///     Known audio file types.
/// </summary>
public enum AudioFileTypes
{
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("wav")]
    Wav,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("mp3")]
    Mp3,
    /// <summary>
    ///     Supported by: OpenAI
    /// </summary>
    [JsonProperty("flac")]
    Flac,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("mp4")]
    Mp4,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("mpeg")]
    Mpeg,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("mpga")]
    Mpga,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("m4a")]
    M4a,
    /// <summary>
    ///     Supported by: OpenAI
    /// </summary>
    [JsonProperty("ogg")]
    Ogg,
    /// <summary>
    ///     Supported by: OpenAI, Groq
    /// </summary>
    [JsonProperty("webm")]
    Webm
}

/// <summary>
///     Audio file object for transcript and translate requests.
/// </summary>
public class AudioFile
{
    /// <summary>
    ///     Stream of the file. Either this or <see cref="Data"/> must be supplied. <see cref="Data"/> has priority over this field.
    /// </summary>
    public Stream? File { get; set; }
    
    /// <summary>
    ///     Data of the file. Either this or <see cref="File"/> must be supplied. This field has priority over <see cref="File"/>.
    /// </summary>
    public byte[]? Data { get; set; }
    
    /// <summary>
    ///     Type of audio file.Must be mp3, mp4, mpeg, mpga, m4a, wav, or webm.
    /// </summary>
    public AudioFileTypes ContentType { get; set; }

    internal string GetContentType => ContentType switch
    {
        AudioFileTypes.Wav => "audio/wav",
        AudioFileTypes.Mp3 => "audio/mpeg",
        AudioFileTypes.Flac => "audio/flac",
        AudioFileTypes.Mpeg => "audio/mpeg",
        AudioFileTypes.Mpga => "audio/mpeg",
        AudioFileTypes.M4a => "audio/mp4",
        AudioFileTypes.Ogg => "audio/ogg",
        AudioFileTypes.Webm => "audio/webm",
        _ => string.Empty
    };
    
    /// <summary>
    ///     Creates an empty audio file
    /// </summary>
    public AudioFile()
    {
        
    }

    /// <summary>
    ///     Creates an audio file from a stream.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    public AudioFile(Stream file, AudioFileTypes type)
    {
        File = file;
        ContentType = type;
    }
    
    /// <summary>
    ///     Creates an audio file from byte array.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public AudioFile(byte[] data, AudioFileTypes type)
    {
        Data = data;
        ContentType = type;
    }
}