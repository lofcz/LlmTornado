namespace LlmTornado.Live;

/// <summary>
/// Controls which realtime input is included in the user's turn.
/// Gemini 3.1 Flash Live defaults to <see cref="TurnIncludesAudioActivityAndAllVideo"/>.
/// </summary>
public enum LiveTurnCoverage
{
    /// <summary>
    /// Provider default for the model.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Activity since the last turn, excluding inactivity (e.g. silence).
    /// </summary>
    TurnIncludesOnlyActivity,

    /// <summary>
    /// All realtime input since the last turn, including inactivity.
    /// </summary>
    TurnIncludesAllInput,

    /// <summary>
    /// Audio activity and all video frames since the last turn (Gemini 3.1 default).
    /// </summary>
    TurnIncludesAudioActivityAndAllVideo
}
