using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// /mdtest — replays a canned markdown document through the streaming renderer in small random
/// chunks, including a simulated tool call and a mid-stream notice, so the full rendering
/// pipeline can be eyeballed without burning tokens.
/// </summary>
internal sealed class MarkdownDemoCommand : ICliCommand
{
    public string Name => "mdtest";
    public string Description => "Replay a canned markdown/tool-call demo through the streaming renderer";
    public string Usage => "/mdtest [fast]";

    private const string Part1 = """
        # Renderer Demo

        This paragraph streams in **small random chunks** to prove that *markdown* markers and
        `inline code` survive arbitrary chunk boundaries. It is also long enough to demonstrate
        width-aware word wrapping with proper hanging behavior across multiple physical lines.

        ## Lists

        - first item with **bold** content
        - second item that is deliberately much longer than one terminal line so the wrap gets a hanging indent under the bullet
          - nested item
        1. ordered one
        2. ordered two

        > A blockquote line with a dim gutter and *emphasis* inside.

        """;

    private const string Part2 = """
        ```csharp
        // code fences render verbatim: **no** inline parsing here
        int Answer(int x) => x * 42;
        ```

        Wide characters wrap by display width: 日本語のテキストと emoji 🎉🚀 も正しく折り返されます。

        ---

        Final paragraph after a horizontal rule. Done!
        """;

    public async Task<bool> ExecuteAsync(string[] args)
    {
        bool fast = args.Contains("fast", StringComparer.OrdinalIgnoreCase);
        int delay = fast ? 0 : 8;
        Random rng = new(1234);

        await StreamChunks(Part1, rng, delay);

        // A notice landing mid-stream must break onto its own line and not bleed styles.
        ConsoleRenderer.WriteInfo("  [context compressed]");

        await StreamChunks("Some text right before a tool call fires.\n", rng, delay);

        // Simulated tool call: draft → invoke → spin → complete.
        FunctionCall call = new()
        {
            Name = "read_file",
            Arguments = "{\"path\":\"src/LlmTornado.Cli/ConsoleRenderer.cs\",\"limit\":120}",
        };
        for (int i = 0; i < 6; i++)
        {
            ConsoleRenderer.WriteToolCallArgumentDelta(call.Name, "{\"chunk\":");
            if (!fast) await Task.Delay(120);
        }
        ConsoleRenderer.OnToolInvoked(call);
        if (!fast) await Task.Delay(1500);
        ConsoleRenderer.OnToolCompleted(call, new FunctionResult(
            "read_file", "120 lines read from ConsoleRenderer.cs", FunctionResultSetContentModes.Passthrough));

        FunctionCall failing = new() { Name = "flaky_tool", Arguments = "{}" };
        ConsoleRenderer.OnToolInvoked(failing);
        if (!fast) await Task.Delay(600);
        ConsoleRenderer.OnToolCompleted(failing, new FunctionResult(
            "flaky_tool", "connection refused by upstream service", FunctionResultSetContentModes.Passthrough, invocationSucceeded: false));

        await StreamChunks(Part2, rng, delay);

        ConsoleRenderer.EndStreamingResponse();
        return true;
    }

    private static async Task StreamChunks(string text, Random rng, int delay)
    {
        int i = 0;
        while (i < text.Length)
        {
            int size = Math.Min(rng.Next(1, 9), text.Length - i);
            ConsoleRenderer.WriteStreamingToken(text.Substring(i, size));
            i += size;
            if (delay > 0) await Task.Delay(delay);
        }
    }
}
