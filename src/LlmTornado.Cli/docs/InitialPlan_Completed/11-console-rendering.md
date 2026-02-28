# Stage 11: Console Rendering

## Goal

Centralize all console output formatting: streaming token display, colored output, tool approval prompts, tables, banners, and progress indicators.

---

## File to Create

### `src/LlmTornado.Cli/ConsoleRenderer.cs`

---

## Color Scheme

| Output Type | Foreground Color | Background |
|-------------|-----------------|------------|
| User prompt | `DarkCyan` | Default |
| Streaming assistant text | `White` | Default |
| Tool call notification | `DarkYellow` | Default |
| Tool auto-approved | `DarkGreen` | Default |
| Tool auto-denied | `DarkRed` | Default |
| Tool approval prompt | `Yellow` | Default |
| Error messages | `Red` | Default |
| Info/status messages | `DarkGray` | Default |
| Skill list (enabled) | `Green` | Default |
| Skill list (disabled) | `DarkGray` | Default |
| Banner | `Cyan` | Default |
| Compressed/summarized | `DarkMagenta` | Default |

---

## ConsoleRenderer — Implementation

```csharp
namespace LlmTornado.Cli;

internal sealed class ConsoleRenderer
{
    private static readonly object _lock = new();
    private static bool _isStreaming;

    // ─── Banner ───

    public static void WriteBanner()
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╭─────────────────────────────────────╮");
        Console.WriteLine("│       LlmTornado CLI Agent          │");
        Console.WriteLine("╰─────────────────────────────────────╯");
        Console.ForegroundColor = originalColor;
        Console.WriteLine();
    }

    // ─── Prompt ───

    public static void WritePrompt(string modelName)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{modelName}> ");
        Console.ForegroundColor = originalColor;
    }

    // ─── Streaming Output ───

    /// <summary>
    /// Write a single token from the streaming response.
    /// Called rapidly during streaming — must be fast.
    /// </summary>
    public static void WriteStreamingToken(string? token)
    {
        if (token is null) return;

        lock (_lock)
        {
            if (!_isStreaming)
            {
                _isStreaming = true;
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.Write(token);
        }
    }

    /// <summary>
    /// Called when streaming completes. Resets console state.
    /// </summary>
    public static void EndStreamingResponse()
    {
        lock (_lock)
        {
            if (_isStreaming)
            {
                _isStreaming = false;
                Console.ResetColor();
                Console.WriteLine();  // Final newline after streamed content
            }
        }
    }

    // ─── Tool Approval ───

    /// <summary>
    /// Display the tool approval prompt box.
    /// </summary>
    public void WriteToolApprovalPrompt(string requestMessage)
    {
        lock (_lock)
        {
            // Pause streaming if it was active
            if (_isStreaming)
            {
                Console.ResetColor();
                Console.WriteLine();
                _isStreaming = false;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╭─ Tool Call Request ────────────────────────╮");

            // Parse and display tool info
            foreach (string line in requestMessage.Split('\n'))
            {
                string padded = line.Length > 42 
                    ? line[..42] 
                    : line.PadRight(42);
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
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"  ✓ [auto-approved] {toolName}");
            Console.ResetColor();
        }
    }

    public void WriteToolAutoDenied(string toolName)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"  ✗ [auto-denied] {toolName}");
            Console.ResetColor();
        }
    }

    // ─── Status Messages ───

    public static void WriteInfo(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void WriteError(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void WriteSuccess(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    // ─── Provider Summary ───

    public static void WriteProviderSummary(ProviderDetectionResult result)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Detected providers:");
        Console.ResetColor();

        foreach (var provider in result.Providers)
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

    // ─── Tables ───

    /// <summary>
    /// Print a simple table with aligned columns.
    /// </summary>
    public static void WriteTable(string[] headers, List<string[]> rows)
    {
        // Calculate column widths
        int[] widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        // Print header
        Console.ForegroundColor = ConsoleColor.White;
        for (int i = 0; i < headers.Length; i++)
            Console.Write($"  {headers[i].PadRight(widths[i] + 2)}");
        Console.WriteLine();

        // Print separator
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int i = 0; i < headers.Length; i++)
            Console.Write($"  {new string('─', widths[i] + 2)}");
        Console.WriteLine();

        // Print rows
        Console.ResetColor();
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                Console.Write($"  {row[i].PadRight(widths[i] + 2)}");
            Console.WriteLine();
        }
        Console.ResetColor();
    }
}
```

---

## Streaming Interaction with Tool Approval

A key complexity: when the agent is streaming text and encounters a tool call mid-stream, we need to:

1. End the current streaming output cleanly
2. Show the tool approval prompt
3. Resume streaming after the tool call completes

This is handled by `WriteToolApprovalPrompt` checking `_isStreaming` and inserting a newline + color reset before showing the prompt. After the tool call, the agent will produce new streaming content that `WriteStreamingToken` handles normally.

```
claude-3-7-sonnet> Review the code in main.py
Let me check the code for issues...

╭─ Tool Call Request ────────────────────────╮
│ Tool: code-review:lint                     │
│ Arguments: {"path": "main.py"}             │
╰────────────────────────────────────────────╯

[1] Allow once
[2] Always allow (remember for this tool)
[3] Deny once
[4] Always deny (remember for this tool)

Choice [1-4]: 2
✓ Tool 'code-review:lint' will be auto-approved in future.

Based on the lint results, I found 3 issues:
1. Unused import on line 5...
2. Missing return type annotation on line 12...
3. Potential SQL injection on line 34...
```

---

## Thread Safety

Streaming tokens arrive on the async continuation of the HTTP response stream. Tool approval prompts happen on the same thread (awaited by TornadoRunner). The `lock (_lock)` ensures console output from different sources doesn't interleave. `Console.Write` itself is thread-safe on .NET 8, but the color changes need to be atomic with the write.

---

## Platform Notes

### Windows

- ANSI escape codes are supported on Windows Terminal and modern ConHost (Windows 10+)
- The implementation uses `Console.ForegroundColor` which works universally
- Box-drawing characters (╭╮╰╯│─) render correctly in Windows Terminal, PowerShell, and cmd with UTF-8 codepage
- If `Console.OutputEncoding` is not UTF-8, the box characters may not render; `Console.OutputEncoding = Encoding.UTF8` should be set at startup

### Linux / macOS

- All terminal emulators support the color scheme used
- Box-drawing characters are universally supported

### Startup Code

```csharp
// In Program.cs, before any output:
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;
```

---

## Future Enhancements

| Enhancement | Description |
|-------------|-------------|
| Markdown rendering | Render markdown in assistant responses (bold, headers, code blocks) using ANSI |
| Syntax highlighting | Highlight code blocks in responses using a lightweight syntax highlighter |
| Spinner / progress | Show a spinner while waiting for first token (thinking indicator) |
| Command autocomplete | Tab-completion for `/` commands using `Console.ReadKey()` |
| Input history | Up/Down arrow to recall previous inputs (readline-style) |
| Width-aware wrapping | Wrap long lines at terminal width instead of letting the OS handle it |

These are not in the initial scope but represent natural extensions of the console rendering system.
