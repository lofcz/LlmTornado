using LlmTornado.Audio.Models;
using LlmTornado.Audio.Models.OpenAi;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado.Audio;

/// <summary>
///     Creates speech request object for tts.
/// </summary>
public class SpeechRequest
{
    /// <summary>
    ///     The string to "convert" into speech
    /// </summary>
    [JsonProperty("input")]
    public string Input { get; set; }

    /// <summary>
    ///     ID of the model to use. Tts1 / tts1hd / gpt4o-mini-tts
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(AudioModelJsonConverter))]
    public AudioModel Model { get; set; } = AudioModel.OpenAi.Tts.Tts1;

    /// <summary>
    ///     Control the voice of your generated audio with additional instructions. Does not work with tts-1 or tts-1-hd.
    /// </summary>
    [JsonProperty("instructions")]
    public string? Instructions { get; set; }
    
    /// <summary>
    ///     The voice to use for tts
    /// </summary>
    [JsonProperty("voice")]
    [JsonConverter(typeof(SpeechVoice.SpeechVoiceJsonConverter))]
    public SpeechVoice Voice { get; set; } = SpeechVoice.Alloy;

    /// <summary>
    ///     The format of the transcript output, in one of these options: mps, opus, aac, flac
    /// </summary>
    [JsonProperty("response_format")]
    [JsonConverter(typeof(SpeechResponseFormat.SpeechResponseFormatJsonConverter))]
    public SpeechResponseFormat ResponseFormat { get; set; } = SpeechResponseFormat.Mp3;

    /// <summary>
    ///     The speed of tts, must be in range [0.25, 4]
    /// </summary>
    [JsonProperty("speed")]
    public float? Speed { get; set; } = 1;
}