namespace LlmTornado.Cli.Rendering;

[Flags]
internal enum StyleFlags
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Dim = 4,
    Underline = 8,
}

/// <summary>A resolved terminal text style: attribute flags plus an optional foreground color.</summary>
internal readonly record struct TextStyle(StyleFlags Flags, ConsoleColor? Fg = null)
{
    public static readonly TextStyle Default = new(StyleFlags.None);

    public TextStyle With(StyleFlags flags) => this with { Flags = Flags | flags };
    public TextStyle Without(StyleFlags flags) => this with { Flags = Flags & ~flags };
}

/// <summary>
/// Style palette for the streaming markdown renderer. Reasoning output uses a dimmed variant
/// of the same theme so both phases share one rendering pipeline.
/// </summary>
internal sealed record MarkdownTheme
{
    public TextStyle Body { get; init; } = new(StyleFlags.None);
    public TextStyle Header { get; init; } = new(StyleFlags.Bold, ConsoleColor.Cyan);
    public TextStyle ListMarker { get; init; } = new(StyleFlags.None, ConsoleColor.DarkCyan);
    public TextStyle Quote { get; init; } = new(StyleFlags.Dim);
    public TextStyle QuoteGutter { get; init; } = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    public TextStyle CodeBlock { get; init; } = new(StyleFlags.None, ConsoleColor.Gray);
    public TextStyle CodeGutter { get; init; } = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    public TextStyle CodeFenceLabel { get; init; } = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    public TextStyle InlineCode { get; init; } = new(StyleFlags.None, ConsoleColor.Yellow);
    public TextStyle HorizontalRule { get; init; } = new(StyleFlags.Dim, ConsoleColor.DarkGray);

    /// <summary>Theme for normal assistant output.</summary>
    public static readonly MarkdownTheme Output = new();

    /// <summary>Dim/italic theme for reasoning ("thinking") output.</summary>
    public static readonly MarkdownTheme Reasoning = new()
    {
        Body = new TextStyle(StyleFlags.Dim | StyleFlags.Italic, ConsoleColor.Gray),
        Header = new TextStyle(StyleFlags.Dim | StyleFlags.Bold, ConsoleColor.Gray),
        ListMarker = new TextStyle(StyleFlags.Dim, ConsoleColor.Gray),
        Quote = new TextStyle(StyleFlags.Dim, ConsoleColor.Gray),
        CodeBlock = new TextStyle(StyleFlags.Dim, ConsoleColor.Gray),
        InlineCode = new TextStyle(StyleFlags.Dim, ConsoleColor.Gray),
    };
}
