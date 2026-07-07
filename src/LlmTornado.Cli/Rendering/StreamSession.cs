using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Coordinates the three interleaved output streams of one agent turn — reasoning, answer text,
/// and tool-call status — inserting separators at phase boundaries and making sure in-place tool
/// status lines are committed before any other text flows. Callers synchronize externally.
/// </summary>
internal sealed class StreamSession
{
    private enum Phase { None, Reasoning, Output, Tool }

    private static readonly TextStyle ThinkingLabelStyle = new(StyleFlags.Dim | StyleFlags.Italic, ConsoleColor.DarkGray);

    private readonly IStyledWriter _writer;
    private readonly StreamingMarkdownRenderer _output;
    private readonly StreamingMarkdownRenderer _reasoning;
    private readonly ToolCallPresenter _tools;

    private Phase _phase = Phase.None;
    private bool _thinkingLabelShown;

    public StreamSession(IStyledWriter writer, ToolCallPresenter tools, Func<int> widthProvider)
    {
        _writer = writer;
        _tools = tools;
        _output = new StreamingMarkdownRenderer(writer, MarkdownTheme.Output, widthProvider);
        _reasoning = new StreamingMarkdownRenderer(writer, MarkdownTheme.Reasoning, widthProvider);
    }

    public bool Active => _phase != Phase.None;

    public void PushOutput(string token)
    {
        EnterPhase(Phase.Output);
        _output.Push(token);
    }

    public void PushReasoning(string token)
    {
        EnterPhase(Phase.Reasoning);
        _reasoning.Push(token);
    }

    public void ToolDrafting(string? toolName)
    {
        EnterPhase(Phase.Tool);
        _tools.OnDrafting(toolName);
    }

    public void ToolInvoked(FunctionCall call)
    {
        EnterPhase(Phase.Tool);
        _tools.OnInvoked(call);
    }

    public void ToolCompleted(FunctionCall call, FunctionResult? result)
    {
        EnterPhase(Phase.Tool);
        _tools.OnCompleted(call, result);
    }

    /// <summary>
    /// Prepares the console for out-of-band output (status notices, prompts, pickers): commits
    /// any open tool status line and finishes any partial streamed line. The stream resumes
    /// cleanly afterwards — styles are re-applied per write, so nothing can bleed.
    /// </summary>
    public void BeginNotice()
    {
        switch (_phase)
        {
            case Phase.Tool:
                _tools.InterruptForOutput();
                break;
            case Phase.Reasoning:
                _reasoning.CompleteLine();
                break;
            case Phase.Output:
                _output.CompleteLine();
                break;
        }
    }

    /// <summary>Ends the turn: flush all partial state, reset renderers, restore default styling.</summary>
    public void End()
    {
        _tools.End();
        _output.CompleteLine();
        _reasoning.CompleteLine();
        _output.Reset();
        _reasoning.Reset();
        _writer.Reset();
        _phase = Phase.None;
        _thinkingLabelShown = false;
    }

    private void EnterPhase(Phase next)
    {
        if (_phase == next) return;

        // Leave the current phase with a clean line, plus a blank separator after prose blocks.
        switch (_phase)
        {
            case Phase.Reasoning:
                _reasoning.CompleteLine();
                _writer.WriteLine();
                break;
            case Phase.Output:
                _output.CompleteLine();
                if (next == Phase.Tool) _writer.WriteLine();
                break;
            case Phase.Tool:
                _tools.InterruptForOutput();
                _writer.WriteLine();
                break;
        }

        _phase = next;

        if (next == Phase.Reasoning && !_thinkingLabelShown)
        {
            _writer.Write("· thinking", ThinkingLabelStyle);
            _writer.WriteLine();
            _thinkingLabelShown = true;
        }
    }
}
