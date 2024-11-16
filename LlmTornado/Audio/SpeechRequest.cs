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
    ///     ID of the model to use. Either <see cref="LlmTornado.Models.Model.TTS_1" /> or <see cref="LlmTornado.Models.Model.TTS_1" />
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; } = LlmTornado.Models.Model.TTS_1;

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