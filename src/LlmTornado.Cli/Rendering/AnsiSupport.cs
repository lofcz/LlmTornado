using System.Runtime.InteropServices;

namespace LlmTornado.Cli.Rendering;

/// <summary>How the terminal can be driven for styled output.</summary>
internal enum RenderCapabilities
{
    /// <summary>No styling at all — output is redirected or the terminal is dumb.</summary>
    Plain,

    /// <summary>Legacy console coloring via <see cref="Console.ForegroundColor"/> only.</summary>
    ConsoleColor,

    /// <summary>Full ANSI/VT escape sequences (SGR styling).</summary>
    Ansi,
}

/// <summary>
/// Enables Windows virtual-terminal processing and detects what styling the output supports.
/// </summary>
internal static class AnsiSupport
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private static bool _vtEnabled;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>
    /// Turns on VT processing for stdout on Windows. Returns true if ANSI sequences will be honored.
    /// On non-Windows platforms this is a no-op returning true (terminals speak VT natively).
    /// </summary>
    public static bool TryEnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows())
        {
            _vtEnabled = true;
            return true;
        }

        try
        {
            IntPtr handle = GetStdHandle(StdOutputHandle);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return false;
            if (!GetConsoleMode(handle, out uint mode)) return false;
            if ((mode & EnableVirtualTerminalProcessing) != 0)
            {
                _vtEnabled = true;
                return true;
            }
            _vtEnabled = SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
            return _vtEnabled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Detects the best rendering mode for the current stdout.</summary>
    public static RenderCapabilities Detect()
    {
        if (Console.IsOutputRedirected) return RenderCapabilities.Plain;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) return RenderCapabilities.Plain;
        if (Environment.GetEnvironmentVariable("TERM") == "dumb") return RenderCapabilities.Plain;

        return _vtEnabled ? RenderCapabilities.Ansi : RenderCapabilities.ConsoleColor;
    }
}
