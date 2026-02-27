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
