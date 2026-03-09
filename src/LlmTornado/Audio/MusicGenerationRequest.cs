using System.Runtime.Serialization;
using LlmTornado.Audio.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Audio;

/// <summary>
/// Request for music generation. Generates a song from lyrics and a style/mood prompt.
/// </summary>
public class MusicGenerationRequest
{
    /// <summary>
    /// The model to use for music generation.
    /// </summary>
    [JsonIgnore]
    public AudioModel? Model { get; set; }
    
    /// <summary>
    /// A description of the music style, mood, and scenario.
    /// For example: "Pop, melancholic, perfect for a rainy night".
    /// Max 2000 characters.
    /// </summary>
    public string? Prompt { get; set; }
    
    /// <summary>
    /// Lyrics of the song. Use \n to separate lines.
    /// Structure tags can be used: [Intro], [Verse], [Pre Chorus], [Chorus], [Interlude],
    /// [Bridge], [Outro], [Post Chorus], [Transition], [Break], [Hook], [Build Up], [Inst], [Solo].
    /// Max 3500 characters.
    /// </summary>
    public string Lyrics { get; set; } = null!;
    
    /// <summary>
    /// Output format of the audio. Default: Hex.
    /// URL links expire after 24 hours.
    /// </summary>
    public MusicOutputFormat? OutputFormat { get; set; }
    
    /// <summary>
    /// Audio output configuration (sample rate, bitrate, file format).
    /// </summary>
    public MusicAudioSetting? AudioSetting { get; set; }
    
    /// <summary>
    /// Creates a music generation request with lyrics.
    /// </summary>
    /// <param name="lyrics">Song lyrics with optional structure tags.</param>
    /// <param name="prompt">Style/mood description.</param>
    /// <param name="model">Model to use. Defaults to music-2.5.</param>
    public MusicGenerationRequest(string lyrics, string? prompt = null, AudioModel? model = null)
    {
        Lyrics = lyrics;
        Prompt = prompt;
        Model = model;
    }
    
    /// <summary>
    /// Parameterless constructor.
    /// </summary>
    public MusicGenerationRequest()
    {
    }
}

/// <summary>
/// Output format for music generation.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MusicOutputFormat
{
    /// <summary>
    /// Audio returned as hex-encoded string.
    /// </summary>
    [EnumMember(Value = "hex")]
    Hex,
    
    /// <summary>
    /// Audio returned as a URL. Expires after 24 hours.
    /// </summary>
    [EnumMember(Value = "url")]
    Url
}

/// <summary>
/// Audio output configuration for music generation.
/// </summary>
public class MusicAudioSetting
{
    /// <summary>
    /// Sampling rate. Options: 16000, 24000, 32000, 44100.
    /// </summary>
    public int? SampleRate { get; set; }
    
    /// <summary>
    /// Bitrate. Options: 32000, 64000, 128000, 256000.
    /// </summary>
    public int? Bitrate { get; set; }
    
    /// <summary>
    /// Audio format. Options: mp3, wav, pcm.
    /// </summary>
    public MusicAudioFormat? Format { get; set; }
}

/// <summary>
/// Audio file format for music output.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum MusicAudioFormat
{
    /// <summary>
    /// MP3 format.
    /// </summary>
    [EnumMember(Value = "mp3")]
    Mp3,
    
    /// <summary>
    /// WAV format.
    /// </summary>
    [EnumMember(Value = "wav")]
    Wav,
    
    /// <summary>
    /// PCM raw format.
    /// </summary>
    [EnumMember(Value = "pcm")]
    Pcm
}
