namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Process-wide rendering configuration, initialized once at startup.
/// Defaults to plain output until <see cref="Initialize"/> runs (e.g. under unit tests).
/// </summary>
internal static class RenderContext
{
    public static RenderCapabilities Capabilities { get; private set; } = RenderCapabilities.Plain;

    /// <summary>True when in-place line rewrites (spinner, status lines) are safe to use.</summary>
    public static bool Interactive => Capabilities != RenderCapabilities.Plain;

    public static void Initialize(RenderCapabilities capabilities)
    {
        Capabilities = capabilities;
    }
}
