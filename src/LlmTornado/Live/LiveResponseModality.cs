namespace LlmTornado.Live;

/// <summary>
/// Output modality for a Gemini Live API session.
/// Native audio Live models support <see cref="Audio"/> only.
/// </summary>
public enum LiveResponseModality
{
    /// <summary>
    /// Text output.
    /// </summary>
    Text,

    /// <summary>
    /// Raw PCM audio output (24 kHz, 16-bit little-endian).
    /// </summary>
    Audio
}
