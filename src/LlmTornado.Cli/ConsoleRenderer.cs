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

    public void WriteQuestionPrompt(InteractiveQuestionDefinition question, int index, int total)
    {
        lock (Lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{index}/{total}] {question.Prompt}");
            Console.ResetColor();

            if (!string.IsNullOrWhiteSpace(question.Description))
                Console.WriteLine(question.Description);

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
