namespace LlmTornado.Live;

/// <summary>
/// Thinking depth for Gemini 3.x Live models. Uses <c>thinkingLevel</c> instead of legacy <c>thinkingBudget</c>.
/// Default for <c>gemini-3.1-flash-live-preview</c> is <see cref="Minimal"/> for lowest latency.
/// </summary>
public enum LiveThinkingLevel
{
    /// <summary>
    /// Minimal reasoning; optimizes for lowest latency.
    /// </summary>
    Minimal,

    /// <summary>
    /// Low reasoning depth.
    /// </summary>
    Low,

    /// <summary>
    /// Balanced reasoning depth.
    /// </summary>
    Medium,

    /// <summary>
    /// Maximum reasoning depth; may increase time-to-first audio token.
    /// </summary>
    High
}
