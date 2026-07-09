using LlmTornado.ChatFunctions;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Rendering;
using LlmTornado.Common;

namespace LlmTornado.Cli;

/// <summary>
/// Centralized console output: streamed markdown answers, tool-call status lines, and formatted
/// prompts. Streaming goes through <see cref="StreamSession"/>; styles are re-applied on every
/// write, so interleaved output can never bleed colors into each other.
/// </summary>
internal sealed class ConsoleRenderer
{
    private static readonly object Lock = new();
    private static IStyledWriter _styledWriter;
    private static ToolCallPresenter _toolPresenter;
    private static StreamSession _session;
    private static Func<string, string?>? _toolServerLookup;

    static ConsoleRenderer()
    {
        (_styledWriter, _toolPresenter, _session) = CreateBackend(AnsiSupport.Detect());
    }

    /// <summary>
    /// Configures the styled-output backend for the detected terminal capabilities.
    /// Called once at startup. <paramref name="toolServerLookup"/> resolves a tool name to the
    /// MCP server it came from (for the server badge on tool status lines).
    /// </summary>
    public static void InitializeRendering(RenderCapabilities capabilities, Func<string, string?>? toolServerLookup = null)
    {
        lock (Lock)
        {
            _toolServerLookup = toolServerLookup;
            _toolPresenter.Dispose();
            (_styledWriter, _toolPresenter, _session) = CreateBackend(capabilities);
        }
    }

    private static (IStyledWriter, ToolCallPresenter, StreamSession) CreateBackend(RenderCapabilities capabilities)
    {
        IStyledWriter writer = StyledWriterFactory.Create(capabilities);
        ToolCallPresenter presenter = new(
            Lock,
            writer,
            SafeWidth,
            name => _toolServerLookup?.Invoke(name),
            interactive: capabilities != RenderCapabilities.Plain);
        StreamSession session = new(writer, presenter, SafeWidth);
        return (writer, presenter, session);
    }

    // ─── Banner ───

    public static void WriteBanner()
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╭─────────────────────────────────────╮");
            Console.WriteLine("│       LlmTornado CLI Agent          │");
            Console.WriteLine("╰─────────────────────────────────────╯");
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    // ─── Prompt ───

    public static void WritePrompt(string modelName)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"{modelName}> ");
            Console.ResetColor();
        }
    }

    // ─── Streaming Output ───

    public static void WriteStreamingToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return;

        lock (Lock)
        {
            _session.PushOutput(token);
        }
    }

    public static void WriteReasoningToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return;

        lock (Lock)
        {
            _session.PushReasoning(token);
        }
    }

    /// <summary>
    /// The model is streaming argument JSON for an upcoming tool call. The raw deltas are not
    /// echoed; a transient "preparing…" status line is shown instead.
    /// </summary>
    public static void WriteToolCallArgumentDelta(string? toolName, string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;

        lock (Lock)
        {
            _session.ToolDrafting(toolName);
        }
    }

    /// <summary>A tool is about to run: show its status line (⏺ name(args) + spinner).</summary>
    public static void OnToolInvoked(FunctionCall call)
    {
        lock (Lock)
        {
            _session.ToolInvoked(call);
        }
    }

    /// <summary>A tool finished: show the result preview line (⎿ preview (duration)).</summary>
    public static void OnToolCompleted(FunctionCall call, FunctionResult? result)
    {
        lock (Lock)
        {
            _session.ToolCompleted(call, result);
        }
    }

    public static void EndStreamingResponse()
    {
        lock (Lock)
        {
            _session.End();
        }
    }

    // ─── Tool Approval ───

    public void WriteToolApprovalPrompt(string requestMessage)
    {
        lock (Lock)
        {
            _session.BeginNotice();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            int consoleWidth = SafeWidth();
            int contentWidth = ToolApprovalContentWidth(consoleWidth);

            if (contentWidth < 8)
            {
                Console.WriteLine("Tool Call Request");
                foreach (string line in requestMessage.Split('\n'))
                {
                    foreach (string wrapped in WrapConsoleLine(line.TrimEnd('\r'), Math.Max(1, consoleWidth - 1)))
                    {
                        Console.WriteLine(wrapped);
                    }
                }
            }
            else
            {
                Console.WriteLine(BuildToolApprovalTopBorder(contentWidth));

                foreach (string line in requestMessage.Split('\n'))
                {
                    foreach (string wrapped in WrapConsoleLine(line.TrimEnd('\r'), contentWidth))
                    {
                        int pad = Math.Max(0, contentWidth - DisplayWidth.Measure(wrapped));
                        Console.WriteLine($"│ {wrapped}{new string(' ', pad)} │");
                    }
                }

                Console.WriteLine($"╰{new string('─', contentWidth + 2)}╯");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[1] Allow once");
            Console.WriteLine("[2] Always allow (remember for this tool)");
            Console.WriteLine("[3] Deny once");
            Console.WriteLine("[4] Always deny (remember for this tool)");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Choice [1-4]: ");
            Console.ResetColor();
        }
    }

    internal static int ToolApprovalContentWidth(int consoleWidth)
    {
        // Leave one spare terminal column so WriteLine cannot auto-wrap before writing the newline.
        int maxContentWidth = Math.Max(1, consoleWidth - 5);
        return Math.Min(100, maxContentWidth);
    }

    private static string BuildToolApprovalTopBorder(int contentWidth)
    {
        const string title = "─ Tool Call Request ";
        int innerWidth = contentWidth + 2;
        string visibleTitle = title.Length > innerWidth ? title[..innerWidth] : title;
        return $"╭{visibleTitle}{new string('─', innerWidth - visibleTitle.Length)}╮";
    }

    private static IEnumerable<string> WrapConsoleLine(string line, int width)
    {
        if (string.IsNullOrEmpty(line))
        {
            yield return "";
            yield break;
        }

        string remaining = line;
        while (DisplayWidth.Measure(remaining) > width)
        {
            // Find the widest prefix that fits (in display columns, not chars), preferring
            // to break at the last space inside it.
            string prefix = DisplayWidth.TruncateToWidth(remaining, width);
            int breakAt = prefix.LastIndexOf(' ');
            if (breakAt <= 0)
                breakAt = prefix.Length;

            yield return remaining[..breakAt];
            remaining = remaining[breakAt..].TrimStart();
        }

        yield return remaining;
    }

    public void WriteQuestionWorkflowStart(AskQuestionsInteractionRequest request)
    {
        lock (Lock)
        {
            _session.BeginNotice();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {request.Title} ===");
            Console.ResetColor();
            if (!string.IsNullOrWhiteSpace(request.Message))
                Console.WriteLine(request.Message);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// A multi-select question is rendered with the interactive arrow/space picker only on a real
    /// terminal. When input is redirected (tests, pipes) we fall back to the numbered line-based UI.
    /// </summary>
    public static bool IsInteractiveMultiSelect(InteractiveQuestionDefinition question) =>
        question.Type == InteractiveQuestionInputType.MultiSelect && !Console.IsInputRedirected;

    public void WriteQuestionPrompt(InteractiveQuestionDefinition question, int index, int total)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{index}/{total}] {question.Prompt}");
            Console.ResetColor();

            if (!string.IsNullOrWhiteSpace(question.Description))
                Console.WriteLine(question.Description);

            // The interactive picker draws (and owns) its own option list.
            if (IsInteractiveMultiSelect(question))
                return;

            for (int optionIndex = 0; optionIndex < question.Options.Count; optionIndex++)
            {
                InteractiveQuestionOption option = question.Options[optionIndex];
                Console.WriteLine($"  [{optionIndex + 1}] {option.Label}");
            }

            if (question.AllowCustomAnswer)
                Console.WriteLine("  [0] Enter a custom answer");

            Console.WriteLine();
        }
    }

    /// <summary>Result of an interactive multi-select: chosen values and whether any was custom-entered.</summary>
    public readonly record struct MultiSelectResult(List<string> Values, bool UsedCustom);

    /// <summary>
    /// Run an interactive multi-select picker: ↑/↓ to move, Space to toggle, Enter to confirm,
    /// C to add a custom value (when allowed), Esc to skip (when not required).
    /// Only call on an interactive terminal — see <see cref="IsInteractiveMultiSelect"/>.
    /// </summary>
    public MultiSelectResult RunMultiSelect(InteractiveQuestionDefinition question)
    {
        List<InteractiveQuestionOption> options = question.Options;
        bool[] selected = new bool[options.Count];
        List<string> customs = [];
        int cursor = 0;
        int scroll = 0;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑/↓ move · Space toggle · Enter confirm"
            + (question.AllowCustomAnswer ? " · C custom" : "")
            + (question.Required ? "" : " · Esc skip"));
        Console.ResetColor();

        // Size the picker to a viewport that fits the window so a long list scrolls in place
        // instead of overflowing the terminal — overflow scrolls the buffer, which would
        // invalidate the absolute row positions we redraw against and duplicate the list.
        int maxRows = Math.Max(3, SafeHeight() - 4); // leave room for the status line + a spare
        int windowRows = Math.Min(Math.Max(options.Count, 1), maxRows);
        int blockHeight = windowRows + 1; // option rows + one status/footer line

        // Reserve the block (scrolling the buffer up if needed), then anchor to a top row that is
        // guaranteed to have the whole block below it inside the window.
        for (int i = 0; i <= blockHeight; i++) Console.WriteLine();
        int listTop = Math.Max(0, Console.CursorTop - blockHeight - 1);

        string? message = null;

        bool? wasVisible = null;
        try { wasVisible = Console.CursorVisible; Console.CursorVisible = false; } catch { /* not supported */ }

        try
        {
            while (true)
            {
                int totalRows = options.Count + customs.Count;

                // Keep the cursor within the visible viewport.
                if (cursor < scroll) scroll = cursor;
                else if (cursor >= scroll + windowRows) scroll = cursor - windowRows + 1;
                if (scroll < 0) scroll = 0;

                DrawMultiSelect(options, selected, customs, cursor, scroll, windowRows, listTop, message);
                message = null;

                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow when totalRows > 0:
                        cursor = (cursor - 1 + totalRows) % totalRows;
                        break;

                    case ConsoleKey.DownArrow when totalRows > 0:
                        cursor = (cursor + 1) % totalRows;
                        break;

                    case ConsoleKey.Spacebar:
                        if (cursor < options.Count)
                        {
                            selected[cursor] = !selected[cursor];
                        }
                        else if (cursor < totalRows)
                        {
                            customs.RemoveAt(cursor - options.Count);
                            if (cursor >= options.Count + customs.Count && cursor > 0)
                                cursor--;
                        }
                        break;

                    case ConsoleKey.C when question.AllowCustomAnswer:
                        string? custom = PromptCustomInline(listTop, blockHeight);
                        if (!string.IsNullOrWhiteSpace(custom))
                        {
                            customs.Add(custom.Trim());
                            cursor = options.Count + customs.Count - 1;
                        }
                        break;

                    case ConsoleKey.Enter:
                        List<string> values =
                        [
                            .. options.Where((_, i) => selected[i]).Select(o => o.Value),
                            .. customs,
                        ];
                        if (question.Required && values.Count == 0)
                        {
                            message = "Select at least one option.";
                            break;
                        }
                        return new MultiSelectResult(values, customs.Count > 0);

                    case ConsoleKey.Escape when !question.Required:
                        return new MultiSelectResult([], false);
                }
            }
        }
        finally
        {
            if (wasVisible is not null)
            {
                try { Console.CursorVisible = wasVisible.Value; } catch { /* ignore */ }
            }
            try { Console.SetCursorPosition(0, Math.Min(listTop + blockHeight, SafeHeight() - 1)); } catch { /* ignore */ }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Repaint the fixed-height viewport in place. Every row is written with absolute positioning
    /// and padded to the window width so stale content is overwritten; nothing is emitted with a
    /// trailing newline, so the buffer never scrolls and <paramref name="listTop"/> stays valid.
    /// </summary>
    private void DrawMultiSelect(
        List<InteractiveQuestionOption> options, bool[] selected, List<string> customs,
        int cursor, int scroll, int windowRows, int listTop, string? message)
    {
        lock (Lock)
        {
            int width = SafeWidth();
            int totalRows = options.Count + customs.Count;

            for (int v = 0; v < windowRows; v++)
            {
                int row = listTop + v;
                int i = scroll + v;

                if (i >= totalRows)
                {
                    if (totalRows == 0 && v == 0)
                        DrawRowText(row, "  (no options — press C to add a custom value)", width, ConsoleColor.DarkGray);
                    else
                        DrawRowText(row, "", width, null);
                    continue;
                }

                bool isCursor = i == cursor;
                if (i < options.Count)
                    DrawRow(row, isCursor, selected[i], options[i].Label, options[i].Description, width);
                else
                    DrawRow(row, isCursor, true, $"{customs[i - options.Count]}  (custom)", null, width);
            }

            // Status / footer line: validation message, or a scroll indicator when the list overflows.
            int footerRow = listTop + windowRows;
            if (message is not null)
            {
                DrawRowText(footerRow, $"  {message}", width, ConsoleColor.Red);
            }
            else if (totalRows > windowRows)
            {
                int first = scroll + 1;
                int last = Math.Min(scroll + windowRows, totalRows);
                DrawRowText(footerRow, $"  ── {first}-{last} of {totalRows} ── ↑/↓ for more ──", width, ConsoleColor.DarkGray);
            }
            else
            {
                DrawRowText(footerRow, "", width, null);
            }
        }
    }

    private static void DrawRow(int row, bool isCursor, bool isSelected, string label, string? description, int width)
    {
        string text = $"  {(isCursor ? ">" : " ")} [{(isSelected ? "x" : " ")}] {label}";
        if (!string.IsNullOrWhiteSpace(description))
            text += $"  — {description}";
        DrawRowText(row, text, width, isCursor ? ConsoleColor.Cyan : null);
    }

    /// <summary>Write a single line at an absolute row, truncated/padded to the window width, no newline.</summary>
    private static void DrawRowText(int row, string text, int width, ConsoleColor? color)
    {
        // Leave the last column untouched so writing the final char can't trigger an auto-wrap/scroll.
        int max = Math.Max(1, width - 1);
        int measured = DisplayWidth.Measure(text);
        text = measured > max
            ? DisplayWidth.TruncateToWidth(text, max - 1) + "…"
            : text + new string(' ', max - measured);

        try { Console.SetCursorPosition(0, row); } catch { /* ignore */ }
        if (color is not null) Console.ForegroundColor = color.Value;
        Console.Write(text);
        if (color is not null) Console.ResetColor();
    }

    private string? PromptCustomInline(int listTop, int blockHeight)
    {
        lock (Lock)
        {
            int width = SafeWidth();
            for (int i = 0; i < blockHeight; i++)
                DrawRowText(listTop + i, "", width, null);
            try { Console.SetCursorPosition(0, listTop); } catch { /* ignore */ }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Custom value: ");
            Console.ResetColor();
        }

        bool? wasVisible = null;
        try { wasVisible = Console.CursorVisible; Console.CursorVisible = true; } catch { /* ignore */ }
        string? value = Console.ReadLine();
        if (wasVisible is not null)
        {
            try { Console.CursorVisible = wasVisible.Value; } catch { /* ignore */ }
        }
        return value;
    }

    private static int SafeWidth()
    {
        try { return Math.Max(1, Console.WindowWidth); }
        catch { return 80; }
    }

    private static int SafeHeight()
    {
        try { return Math.Max(6, Console.WindowHeight); }
        catch { return 24; }
    }

    public void WriteQuestionInputHint(InteractiveQuestionDefinition question)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string prompt = question.Type switch
            {
                InteractiveQuestionInputType.SingleChoice => question.Required ? "Select one option: " : "Select one option or press Enter to skip: ",
                InteractiveQuestionInputType.MultiSelect => question.Required ? "Select one or more options (comma-separated): " : "Select options (comma-separated) or press Enter to skip: ",
                InteractiveQuestionInputType.YesNo => question.Required ? "Answer [y/n]: " : "Answer [y/n] or press Enter to skip: ",
                InteractiveQuestionInputType.Number => question.Required ? "Enter a number: " : "Enter a number or press Enter to skip: ",
                _ => question.Required ? "Answer: " : "Answer (press Enter to skip): ",
            };
            Console.Write(prompt);
            Console.ResetColor();
        }
    }

    public void WriteToolAutoApproved(string toolName)
    {
        lock (Lock)
        {
            _session.BeginNotice();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"  ✓ [auto-approved] {toolName}");
            Console.ResetColor();
        }
    }

    public void WriteToolAutoDenied(string toolName)
    {
        lock (Lock)
        {
            _session.BeginNotice();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"  ✗ [auto-denied] {toolName}");
            Console.ResetColor();
        }
    }

    // ─── Status Messages ───

    /// <summary>
    /// Prepares the console for an out-of-band message: commits any open tool status line and
    /// finishes any partial streamed line so the notice starts at column 0. Streaming resumes
    /// cleanly afterwards because styles are re-applied on every write.
    /// </summary>
    private static void WriteNotice(string message, TextStyle style)
    {
        lock (Lock)
        {
            _session.BeginNotice();
            _styledWriter.Write(message, style);
            _styledWriter.WriteLine();
            _styledWriter.Reset();
        }
    }

    public static void WriteInfo(string message) =>
        WriteNotice(message, new TextStyle(StyleFlags.Dim, ConsoleColor.DarkGray));

    public static void WriteError(string message) =>
        WriteNotice(message, new TextStyle(StyleFlags.None, ConsoleColor.Red));

    public static void WriteWarning(string message) =>
        WriteNotice(message, new TextStyle(StyleFlags.None, ConsoleColor.Yellow));

    public static void WriteSuccess(string message) =>
        WriteNotice(message, new TextStyle(StyleFlags.None, ConsoleColor.Green));

    /// <summary>One-line dim status footer (context/token gauge after each turn).</summary>
    public static void WriteDimStatus(string message) =>
        WriteNotice(message, new TextStyle(StyleFlags.Dim, ConsoleColor.DarkGray));

    // ─── Tool Optimization ───

    public static void WriteToolOptimization(int totalCount, int selectedCount)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  [optimizing tools: {totalCount} → {selectedCount}]");
            Console.ResetColor();
        }
    }

    public static void WriteToolOptimizationSkipped(int count, string reason)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  [tool optimization skipped: using all {count} tools — {reason}]");
            Console.ResetColor();
        }
    }

    // ─── Provider Summary ───

    public static void WriteProviderSummary(ProviderDetectionResult result)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Detected providers:");
            Console.ResetColor();

            foreach (DetectedProvider provider in result.Providers)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  ✓ ");
                Console.ForegroundColor = ConsoleColor.White;
                string label = provider.EndpointName is not null
                    ? $"{provider.Provider} [{provider.EndpointName}]"
                    : provider.Provider.ToString();
                Console.Write(label);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" — {provider.Models.Count} models");
            }
            Console.ResetColor();
        }
    }
}
