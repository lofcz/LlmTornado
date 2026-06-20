using LlmTornado.Cli.Core.Providers;
using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli;

/// <summary>
/// Centralized console output with color coding, streaming support, and formatted prompts.
/// </summary>
internal sealed class ConsoleRenderer
{
    private static readonly object Lock = new();
    private static bool _isStreaming;

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
        if (token is null) return;

        lock (Lock)
        {
            if (!_isStreaming)
            {
                _isStreaming = true;
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.Write(token);
        }
    }

    public static void EndStreamingResponse()
    {
        lock (Lock)
        {
            if (_isStreaming)
            {
                _isStreaming = false;
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }

    // ─── Tool Approval ───

    public void WriteToolApprovalPrompt(string requestMessage)
    {
        lock (Lock)
        {
            if (_isStreaming)
            {
                Console.ResetColor();
                Console.WriteLine();
                _isStreaming = false;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╭─ Tool Call Request ────────────────────────╮");

            foreach (string line in requestMessage.Split('\n'))
            {
                string padded = line.Length > 42 ? line[..42] : line.PadRight(42);
                Console.WriteLine($"│ {padded} │");
            }

            Console.WriteLine("╰────────────────────────────────────────────╯");
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

    public void WriteQuestionWorkflowStart(AskQuestionsInteractionRequest request)
    {
        lock (Lock)
        {
            if (_isStreaming)
            {
                Console.ResetColor();
                Console.WriteLine();
                _isStreaming = false;
            }

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

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑/↓ move · Space toggle · Enter confirm"
            + (question.AllowCustomAnswer ? " · C custom" : "")
            + (question.Required ? "" : " · Esc skip"));
        Console.ResetColor();

        int listTop = Console.CursorTop;
        int drawn = 0;
        string? message = null;

        bool? wasVisible = null;
        try { wasVisible = Console.CursorVisible; Console.CursorVisible = false; } catch { /* not supported */ }

        try
        {
            while (true)
            {
                drawn = DrawMultiSelect(options, selected, customs, cursor, listTop, drawn, message);
                message = null;

                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                int totalRows = options.Count + customs.Count;

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
                        string? custom = PromptCustomInline(listTop, drawn);
                        if (!string.IsNullOrWhiteSpace(custom))
                        {
                            customs.Add(custom.Trim());
                            cursor = options.Count + customs.Count - 1;
                        }
                        drawn = 2; // force the prompt line(s) to be cleared on next draw
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
            try { Console.SetCursorPosition(0, listTop + drawn); } catch { /* ignore */ }
            Console.WriteLine();
        }
    }

    private int DrawMultiSelect(
        List<InteractiveQuestionOption> options, bool[] selected, List<string> customs,
        int cursor, int listTop, int prevLineCount, string? message)
    {
        lock (Lock)
        {
            int width = SafeWidth();

            // Clear the region drawn last time so shrinking lists don't leave stragglers.
            for (int i = 0; i < prevLineCount; i++)
            {
                try { Console.SetCursorPosition(0, listTop + i); } catch { /* ignore */ }
                Console.Write(new string(' ', width));
            }
            try { Console.SetCursorPosition(0, listTop); } catch { /* ignore */ }

            int line = 0;
            if (options.Count + customs.Count == 0)
            {
                Console.WriteLine("  (no options — press C to add a custom value)");
                line++;
            }

            for (int i = 0; i < options.Count; i++)
            {
                DrawRow(i == cursor, selected[i], options[i].Label, options[i].Description, width);
                line++;
            }
            for (int i = 0; i < customs.Count; i++)
            {
                DrawRow(options.Count + i == cursor, true, $"{customs[i]}  (custom)", null, width);
                line++;
            }

            if (message is not null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  {message}");
                Console.ResetColor();
                line++;
            }

            return line;
        }
    }

    private static void DrawRow(bool isCursor, bool isSelected, string label, string? description, int width)
    {
        if (isCursor)
            Console.ForegroundColor = ConsoleColor.Cyan;

        string text = $"  {(isCursor ? ">" : " ")} [{(isSelected ? "x" : " ")}] {label}";
        if (!string.IsNullOrWhiteSpace(description))
            text += $"  — {description}";
        if (text.Length > width - 1)
            text = text[..(width - 2)] + "…";

        Console.WriteLine(text);

        if (isCursor)
            Console.ResetColor();
    }

    private string? PromptCustomInline(int listTop, int prevLineCount)
    {
        lock (Lock)
        {
            int width = SafeWidth();
            for (int i = 0; i < prevLineCount; i++)
            {
                try { Console.SetCursorPosition(0, listTop + i); } catch { /* ignore */ }
                Console.Write(new string(' ', width));
            }
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
        try { return Math.Max(20, Console.WindowWidth); }
        catch { return 80; }
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
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"  ✓ [auto-approved] {toolName}");
            Console.ResetColor();
        }
    }

    public void WriteToolAutoDenied(string toolName)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"  ✗ [auto-denied] {toolName}");
            Console.ResetColor();
        }
    }

    // ─── Status Messages ───

    public static void WriteInfo(string message)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void WriteError(string message)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void WriteSuccess(string message)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

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
                Console.Write($"{provider.Provider}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" — {provider.Models.Count} models");
            }
            Console.ResetColor();
        }
    }
}
