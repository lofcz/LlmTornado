namespace LlmTornado.Interactions;

/// <summary>
/// Gemini Interactions API schema revision selected via the <c>Api-Revision</c> request header.
/// Legacy schema is removed on June 8, 2026.
/// </summary>
public enum InteractionSchemaRevision
{
    /// <summary>
    /// New steps-based schema (<c>Api-Revision: 2026-05-20</c>). Default after May 26, 2026.
    /// </summary>
    May2026,

    /// <summary>
    /// Legacy outputs-based schema (<c>Api-Revision: 2026-05-07</c>). Removed June 8, 2026.
    /// </summary>
    LegacyMay2026
}

internal static class InteractionSchemaRevisionExtensions
{
    internal static string? ToHeaderValue(this InteractionSchemaRevision revision) => revision switch
    {
        InteractionSchemaRevision.May2026 => "2026-05-20",
        InteractionSchemaRevision.LegacyMay2026 => "2026-05-07",
        _ => null
    };
}
