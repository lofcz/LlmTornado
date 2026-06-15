using LlmTornado.Audio.Models;
using LlmTornado.Chat.Models;

namespace LlmTornado.Realtime.Translation;

/// <summary>
/// Input noise reduction mode for Realtime translation sessions.
/// </summary>
public enum RealtimeTranslationNoiseReduction
{
    /// <summary>
    /// Close-talking microphones such as headphones.
    /// </summary>
    NearField,

    /// <summary>
    /// Far-field microphones such as laptop or conference room microphones.
    /// </summary>
    FarField
}

/// <summary>
/// Session configuration for a Realtime translation WebSocket connection.
/// </summary>
public class RealtimeTranslationSessionConfig
{
    /// <summary>
    /// Target language for translated output audio and transcript deltas (ISO 639-1 code, e.g. <c>es</c>, <c>fr</c>).
    /// </summary>
    public string? OutputLanguage { get; set; }

    /// <summary>
    /// When set, the server emits <c>session.input_transcript.delta</c> events for source-language transcription.
    /// </summary>
    public ChatModel? InputTranscriptionModel { get; set; }

    /// <summary>
    /// Optional input noise reduction. Set to <see langword="null"/> to disable.
    /// </summary>
    public RealtimeTranslationNoiseReduction? NoiseReduction { get; set; }
}
