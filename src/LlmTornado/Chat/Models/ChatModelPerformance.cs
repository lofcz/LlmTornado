namespace LlmTornado.Chat.Models;

/// <summary>
/// Relative performance tier of a "latest" model pointer.
/// </summary>
public enum ChatModelPerformance
{
    /// <summary>
    /// Absolute top-end tier (pro/extended-compute models); slowest and most expensive.
    /// </summary>
    Max,

    /// <summary>
    /// Flagship / frontier tier.
    /// </summary>
    Large,

    /// <summary>
    /// Balanced intelligence/cost tier.
    /// </summary>
    Medium,

    /// <summary>
    /// Cheap, fast, high-volume tier.
    /// </summary>
    Small
}
