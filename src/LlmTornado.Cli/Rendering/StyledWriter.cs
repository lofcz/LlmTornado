using System.Text;

namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Sink for styled text. Implementations re-apply the style on every write, so callers never
/// need to track "current color" state — interleaved writers cannot bleed styles into each other.
/// </summary>
internal interface IStyledWriter
{
    void Write(string text, TextStyle style);
    void WriteLine();
    void Reset();
}

internal static class StyledWriterFactory
{
    /// <summary>
    /// Creates a writer for the given capabilities. When <paramref name="output"/> is null the
    /// writer targets <see cref="Console.Out"/> dynamically (respects later Console.SetOut calls).
    /// </summary>
    public static IStyledWriter Create(RenderCapabilities capabilities, TextWriter? output = null)
    {
        return capabilities switch
        {
            RenderCapabilities.Ansi => new AnsiStyledWriter(output),
            RenderCapabilities.ConsoleColor => new ConsoleColorStyledWriter(output),
            _ => new PlainStyledWriter(output),
        };
    }
}

/// <summary>Emits SGR escape sequences, diffing against the last applied style.</summary>
internal sealed class AnsiStyledWriter(TextWriter? output = null) : IStyledWriter
{
    private TextStyle? _current;

    private TextWriter Out => output ?? Console.Out;

    public void Write(string text, TextStyle style)
    {
        if (_current != style)
        {
            Out.Write(BuildSgr(style));
            _current = style;
        }
        Out.Write(text);
    }

    public void WriteLine()
    {
        Out.WriteLine();
    }

    public void Reset()
    {
        if (_current is not null)
        {
            Out.Write("\x1b[0m");
            _current = null;
        }
    }

    private static string BuildSgr(TextStyle style)
    {
        StringBuilder sb = new("\x1b[0");
        if (style.Flags.HasFlag(StyleFlags.Bold)) sb.Append(";1");
        if (style.Flags.HasFlag(StyleFlags.Dim)) sb.Append(";2");
        if (style.Flags.HasFlag(StyleFlags.Italic)) sb.Append(";3");
        if (style.Flags.HasFlag(StyleFlags.Underline)) sb.Append(";4");
        if (style.Fg is { } fg) sb.Append(';').Append(ForegroundCode(fg));
        return sb.Append('m').ToString();
    }

    private static int ForegroundCode(ConsoleColor color) => color switch
    {
        ConsoleColor.Black => 30,
        ConsoleColor.DarkRed => 31,
        ConsoleColor.DarkGreen => 32,
        ConsoleColor.DarkYellow => 33,
        ConsoleColor.DarkBlue => 34,
        ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkCyan => 36,
        ConsoleColor.Gray => 37,
        ConsoleColor.DarkGray => 90,
        ConsoleColor.Red => 91,
        ConsoleColor.Green => 92,
        ConsoleColor.Yellow => 93,
        ConsoleColor.Blue => 94,
        ConsoleColor.Magenta => 95,
        ConsoleColor.Cyan => 96,
        ConsoleColor.White => 97,
        _ => 39,
    };
}

/// <summary>Legacy conhost fallback: maps styles onto <see cref="Console.ForegroundColor"/>.</summary>
internal sealed class ConsoleColorStyledWriter(TextWriter? output = null) : IStyledWriter
{
    private TextWriter Out => output ?? Console.Out;

    public void Write(string text, TextStyle style)
    {
        ConsoleColor? fg = style.Fg;
        if (fg is null && style.Flags.HasFlag(StyleFlags.Dim)) fg = ConsoleColor.DarkGray;
        else if (fg is not null && style.Flags.HasFlag(StyleFlags.Bold)) fg = Brighten(fg.Value);

        if (fg is { } color)
        {
            Console.ForegroundColor = color;
            Out.Write(text);
            Console.ResetColor();
        }
        else
        {
            Out.Write(text);
        }
    }

    public void WriteLine()
    {
        Out.WriteLine();
    }

    public void Reset()
    {
        Console.ResetColor();
    }

    private static ConsoleColor Brighten(ConsoleColor color) => color switch
    {
        ConsoleColor.DarkRed => ConsoleColor.Red,
        ConsoleColor.DarkGreen => ConsoleColor.Green,
        ConsoleColor.DarkYellow => ConsoleColor.Yellow,
        ConsoleColor.DarkBlue => ConsoleColor.Blue,
        ConsoleColor.DarkMagenta => ConsoleColor.Magenta,
        ConsoleColor.DarkCyan => ConsoleColor.Cyan,
        ConsoleColor.DarkGray => ConsoleColor.Gray,
        ConsoleColor.Gray => ConsoleColor.White,
        _ => color,
    };
}

/// <summary>No styling at all — for redirected output and dumb terminals.</summary>
internal sealed class PlainStyledWriter(TextWriter? output = null) : IStyledWriter
{
    private TextWriter Out => output ?? Console.Out;

    public void Write(string text, TextStyle style) => Out.Write(text);

    public void WriteLine() => Out.WriteLine();

    public void Reset()
    {
    }
}
