using System.Text;
using LlmTornado.Cli.Commands;

namespace LlmTornado.Cli.Input;

/// <summary>
/// What, if anything, the editor should complete at the current cursor position.
/// </summary>
internal enum CompletionKind
{
    None,
    Command,
    File,
}

/// <summary>
/// The completion context detected for a buffer + cursor position. <see cref="TokenStart"/>
/// and <see cref="TokenEnd"/> bound the token that an accepted suggestion replaces.
/// </summary>
internal readonly record struct CompletionContext(CompletionKind Kind, int TokenStart, int TokenEnd, string Partial)
{
    public static CompletionContext None => new(CompletionKind.None, 0, 0, "");
}

/// <summary>
/// A single suggestion shown in the autocomplete menu.
/// </summary>
internal readonly record struct EditorSuggestion(string Display, string Detail, string Value);

/// <summary>
/// The visible slice of the input buffer for the current terminal width.
/// </summary>
internal readonly record struct InputViewport(int StartIndex, string Text, int CursorColumn);

/// <summary>
/// A custom single-line input reader (replacement for <see cref="Console.ReadLine"/>) that
/// renders live autocomplete suggestions below the prompt: <c>/commands</c> when the line
/// starts with <c>/</c>, and <c>@documents</c> when the current token starts with <c>@</c>.
/// </summary>
/// <remarks>
/// On a non-interactive (redirected) input stream it falls back to <see cref="Console.ReadLine"/>
/// so piped input and tests behave exactly as before.
/// </remarks>
internal sealed class LineEditor
{
    private const int MaxVisibleSuggestions = 8;

    private readonly IReadOnlyDictionary<string, ICliCommand> _commands;
    private readonly Func<string, IReadOnlyList<string>> _fileSuggester;

    public LineEditor(
        IReadOnlyDictionary<string, ICliCommand> commands,
        Func<string, IReadOnlyList<string>> fileSuggester)
    {
        _commands = commands;
        _fileSuggester = fileSuggester;
    }

    /// <summary>
    /// Read a line of input, showing the prompt and live completion menu. Returns the entered
    /// text, or <c>null</c> on end-of-input.
    /// </summary>
    public string? ReadLine(string modelName)
    {
        if (Console.IsInputRedirected)
        {
            ConsoleRenderer.WritePrompt(modelName);
            return Console.ReadLine();
        }

        string promptText = $"{modelName}> ";

        StringBuilder buffer = new();
        int cursor = 0;
        int menuIndex = 0;
        bool suppressMenu = false;

        List<EditorSuggestion> suggestions = [];

        int inputTop = SafeCursorTop();
        int lastMenuRows = 0;

        bool? wasCursorVisible = null;
        try { wasCursorVisible = Console.CursorVisible; } catch { /* unsupported */ }

        void Recompute(bool resetIndex)
        {
            suggestions = BuildSuggestions(buffer.ToString(), cursor);
            if (resetIndex)
                menuIndex = 0;
            else if (suggestions.Count > 0)
                menuIndex = Math.Clamp(menuIndex, 0, suggestions.Count - 1);
        }

        bool MenuOpen() => !suppressMenu && suggestions.Count > 0;

        void Draw()
        {
            int width = SafeWidth();
            int safeLineWidth = Math.Max(1, width - 1);
            string promptDisplay = FitPrompt(promptText, safeLineWidth);
            int inputWidth = Math.Max(0, safeLineWidth - promptDisplay.Length);
            InputViewport viewport = ComputeViewport(buffer.ToString(), cursor, inputWidth);

            try { Console.CursorVisible = false; } catch { /* ignore */ }

            // Clear the input line and any previously drawn menu rows.
            int linesToClear = 1 + lastMenuRows;
            for (int i = 0; i < linesToClear; i++)
            {
                if (!TrySetCursor(0, inputTop + i)) break;
                Console.Write(new string(' ', Math.Max(0, width - 1)));
            }

            // Input line: prompt + buffer.
            TrySetCursor(0, inputTop);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(promptDisplay);
            Console.ResetColor();
            Console.Write(viewport.Text);

            // Menu rows.
            int drawnRows = 0;
            if (MenuOpen())
                drawnRows = DrawMenu(suggestions, menuIndex, width);

            // Recompute the input row in case writing the menu scrolled the buffer.
            int endTop = SafeCursorTop();
            inputTop = endTop - drawnRows;
            lastMenuRows = drawnRows;

            // Place the cursor within the input line (single-line assumption).
            int col = Math.Min(promptDisplay.Length + viewport.CursorColumn, width - 1);
            TrySetCursor(col, inputTop);
            try { Console.CursorVisible = true; } catch { /* ignore */ }
        }

        Recompute(resetIndex: true);
        Draw();

        try
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        if (MenuOpen())
                        {
                            cursor = Accept(buffer, suggestions[menuIndex], cursor);
                            Recompute(resetIndex: true);
                            suppressMenu = true; // keep closed until next edit
                            Draw();
                            break;
                        }
                        Finish();
                        return buffer.ToString();

                    case ConsoleKey.Tab:
                        if (MenuOpen())
                        {
                            cursor = Accept(buffer, suggestions[menuIndex], cursor);
                            Recompute(resetIndex: true);
                            suppressMenu = true;
                            Draw();
                        }
                        break;

                    case ConsoleKey.Escape:
                        if (MenuOpen())
                            suppressMenu = true;
                        else
                        {
                            buffer.Clear();
                            cursor = 0;
                            Recompute(resetIndex: true);
                        }
                        Draw();
                        break;

                    case ConsoleKey.UpArrow:
                        if (MenuOpen())
                        {
                            menuIndex = (menuIndex - 1 + suggestions.Count) % suggestions.Count;
                            Draw();
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (MenuOpen())
                        {
                            menuIndex = (menuIndex + 1) % suggestions.Count;
                            Draw();
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        if (cursor > 0) cursor--;
                        Recompute(resetIndex: true);
                        Draw();
                        break;

                    case ConsoleKey.RightArrow:
                        if (cursor < buffer.Length) cursor++;
                        Recompute(resetIndex: true);
                        Draw();
                        break;

                    case ConsoleKey.Home:
                        cursor = 0;
                        Recompute(resetIndex: true);
                        Draw();
                        break;

                    case ConsoleKey.End:
                        cursor = buffer.Length;
                        Recompute(resetIndex: true);
                        Draw();
                        break;

                    case ConsoleKey.Backspace:
                        if (cursor > 0)
                        {
                            buffer.Remove(cursor - 1, 1);
                            cursor--;
                            suppressMenu = false;
                            Recompute(resetIndex: true);
                        }
                        Draw();
                        break;

                    case ConsoleKey.Delete:
                        if (cursor < buffer.Length)
                        {
                            buffer.Remove(cursor, 1);
                            suppressMenu = false;
                            Recompute(resetIndex: true);
                        }
                        Draw();
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
                        {
                            buffer.Insert(cursor, key.KeyChar);
                            cursor++;
                            suppressMenu = false;
                            Recompute(resetIndex: true);
                            Draw();
                        }
                        break;
                }
            }
        }
        finally
        {
            if (wasCursorVisible is not null)
            {
                try { Console.CursorVisible = wasCursorVisible.Value; } catch { /* ignore */ }
            }
        }

        // Clear the menu, move past the input line, and emit a newline so subsequent
        // output starts cleanly below the entered text.
        void Finish()
        {
            suppressMenu = true;
            Draw();
            int width = SafeWidth();
            int safeLineWidth = Math.Max(1, width - 1);
            string promptDisplay = FitPrompt(promptText, safeLineWidth);
            int inputWidth = Math.Max(0, safeLineWidth - promptDisplay.Length);
            InputViewport endViewport = ComputeViewport(buffer.ToString(), buffer.Length, inputWidth);
            int endCol = Math.Min(promptDisplay.Length + endViewport.CursorColumn, width - 1);
            TrySetCursor(endCol, inputTop);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    /// <summary>Build the suggestion list for the current buffer/cursor.</summary>
    private List<EditorSuggestion> BuildSuggestions(string buffer, int cursor)
    {
        CompletionContext ctx = DetectCompletion(buffer, cursor);

        switch (ctx.Kind)
        {
            case CompletionKind.Command:
                return _commands.Values
                    .Where(c => c.Name.StartsWith(ctx.Partial, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(c => new EditorSuggestion($"/{c.Name}", c.Description, $"/{c.Name}"))
                    .ToList();

            case CompletionKind.File:
                return _fileSuggester(ctx.Partial)
                    .Select(p => new EditorSuggestion(p, "", p))
                    .ToList();

            default:
                return [];
        }
    }

    /// <summary>
    /// Detect whether the cursor sits in a <c>/command</c> or <c>@file</c> completion context.
    /// Pure and deterministic — the unit-testable core of the editor.
    /// </summary>
    internal static CompletionContext DetectCompletion(string buffer, int cursor)
    {
        if (string.IsNullOrEmpty(buffer))
            return CompletionContext.None;

        cursor = Math.Clamp(cursor, 0, buffer.Length);

        // Command: first non-whitespace char is '/', and the cursor is within the first token.
        int firstNonWs = 0;
        while (firstNonWs < buffer.Length && char.IsWhiteSpace(buffer[firstNonWs]))
            firstNonWs++;

        if (firstNonWs < buffer.Length && buffer[firstNonWs] == '/')
        {
            int tokenEnd = firstNonWs;
            while (tokenEnd < buffer.Length && !char.IsWhiteSpace(buffer[tokenEnd]))
                tokenEnd++;

            if (cursor > firstNonWs && cursor <= tokenEnd)
            {
                string partial = buffer[(firstNonWs + 1)..tokenEnd];
                return new CompletionContext(CompletionKind.Command, firstNonWs, tokenEnd, partial);
            }
        }

        // File: the whitespace-delimited token containing the cursor begins with '@'.
        int start = cursor;
        while (start > 0 && !char.IsWhiteSpace(buffer[start - 1]))
            start--;

        int end = cursor;
        while (end < buffer.Length && !char.IsWhiteSpace(buffer[end]))
            end++;

        if (start < buffer.Length && buffer[start] == '@')
        {
            string raw = buffer[(start + 1)..end];
            if (raw.StartsWith('"'))
                raw = raw[1..];
            return new CompletionContext(CompletionKind.File, start, end, raw);
        }

        return CompletionContext.None;
    }

    /// <summary>
    /// Apply a suggestion to the buffer, replacing the context token. Returns the new cursor
    /// position (end of the inserted text). Pure helper used by the editor and tests.
    /// </summary>
    internal static int ApplyCompletion(StringBuilder buffer, CompletionContext ctx, string value)
    {
        string replacement = ctx.Kind switch
        {
            CompletionKind.Command => $"{value} ",
            CompletionKind.File => $"@{QuoteIfNeeded(value)} ",
            _ => value,
        };

        buffer.Remove(ctx.TokenStart, ctx.TokenEnd - ctx.TokenStart);
        buffer.Insert(ctx.TokenStart, replacement);
        return ctx.TokenStart + replacement.Length;
    }

    private int Accept(StringBuilder buffer, EditorSuggestion suggestion, int cursor)
    {
        CompletionContext ctx = DetectCompletion(buffer.ToString(), cursor);
        if (ctx.Kind == CompletionKind.None)
            return cursor;
        return ApplyCompletion(buffer, ctx, suggestion.Value);
    }

    private static string QuoteIfNeeded(string path)
        => path.Contains(' ') ? $"\"{path}\"" : path;

    /// <summary>Draw the suggestion menu below the input line. Returns the number of rows drawn.</summary>
    private static int DrawMenu(List<EditorSuggestion> suggestions, int selected, int width)
    {
        int count = suggestions.Count;
        int visible = Math.Min(MaxVisibleSuggestions, count);

        // Scroll window centered on the selected item.
        int start = Math.Clamp(selected - visible / 2, 0, Math.Max(0, count - visible));

        int rows = 0;
        for (int i = start; i < start + visible; i++)
        {
            Console.Write('\n');
            rows++;

            bool isSel = i == selected;
            EditorSuggestion s = suggestions[i];

            string line = s.Detail.Length > 0 ? $"  {s.Display}   {s.Detail}" : $"  {s.Display}";
            if (line.Length > width - 1)
                line = line[..(width - 1)];

            if (isSel)
            {
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write(line.PadRight(Math.Max(0, width - 1)));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(line);
                Console.ResetColor();
            }
        }

        if (count > visible)
        {
            Console.Write('\n');
            rows++;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  … (+{count - visible} more)");
            Console.ResetColor();
        }

        return rows;
    }

    private static int SafeWidth()
    {
        try { return Math.Max(20, Console.WindowWidth); }
        catch { return 80; }
    }

    private static int SafeCursorTop()
    {
        try { return Console.CursorTop; }
        catch { return 0; }
    }

    /// <summary>
    /// Keep input rendering inside a single terminal row by exposing only the visible window.
    /// </summary>
    internal static InputViewport ComputeViewport(string buffer, int cursor, int width)
    {
        string value = buffer ?? string.Empty;
        int clampedCursor = Math.Clamp(cursor, 0, value.Length);

        if (width <= 0)
            return new InputViewport(0, string.Empty, 0);

        if (value.Length <= width)
            return new InputViewport(0, value, clampedCursor);

        int maxStart = value.Length - width;
        int start = Math.Clamp(clampedCursor - width + 1, 0, maxStart);
        int length = Math.Min(width, value.Length - start);
        string visible = value.Substring(start, length);
        int cursorColumn = Math.Clamp(clampedCursor - start, 0, visible.Length);

        return new InputViewport(start, visible, cursorColumn);
    }

    private static string FitPrompt(string prompt, int safeWidth)
    {
        string value = prompt ?? string.Empty;
        if (safeWidth <= 0)
            return string.Empty;

        if (value.Length <= safeWidth)
            return value;

        // Keep the right side so the "> " marker stays visible on narrow terminals.
        return value[^safeWidth..];
    }

    private static bool TrySetCursor(int left, int top)
    {
        try
        {
            if (top < 0) return false;
            Console.SetCursorPosition(Math.Max(0, left), top);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
