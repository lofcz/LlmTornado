namespace LlmTornado.Live;

/// <summary>
/// Media resolution for Live API input frames.
/// </summary>
public enum LiveMediaResolution
{
    /// <summary>
    /// Provider default.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Low resolution (64 tokens).
    /// </summary>
    Low,

    /// <summary>
    /// Medium resolution (256 tokens).
    /// </summary>
    Medium,

    /// <summary>
    /// High resolution.
    /// </summary>
    High
}
