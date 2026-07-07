using System.Diagnostics;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Renders the lifecycle of tool calls as compact status lines:
///   ⏺ read_file(path: "src/foo.cs")  ⠋ 2s      ← animated while running (interactive only)
///     ⎿ 120 lines (1.3s)                        ← result preview on completion
/// Owns a single rewritable console line at a time; any other output must call
/// <see cref="InterruptForOutput"/> first so the line is committed before new text flows.
/// All public members must be called under the shared renderer lock passed to the constructor.
/// </summary>
internal sealed class ToolCallPresenter : IDisposable
{
    private static readonly char[] SpinnerFrames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

    private static readonly TextStyle DotStyle = new(StyleFlags.None, ConsoleColor.Cyan);
    private static readonly TextStyle NameStyle = new(StyleFlags.Bold);
    private static readonly TextStyle ArgsStyle = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    private static readonly TextStyle SpinStyle = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    private static readonly TextStyle ResultStyle = new(StyleFlags.Dim, ConsoleColor.DarkGray);
    private static readonly TextStyle ErrorStyle = new(StyleFlags.None, ConsoleColor.Red);
    private static readonly TextStyle DraftStyle = new(StyleFlags.Dim, ConsoleColor.DarkYellow);

    private sealed class CallState
    {
        public required string Display;
        public required string ArgsSummary;
        public Stopwatch? Watch;
    }

    private readonly object _sync;
    private readonly IStyledWriter _writer;
    private readonly Func<int> _widthProvider;
    private readonly Func<string, string?>? _serverLookup;
    private readonly bool _interactive;
    private readonly bool _enableTimer;

    private readonly Dictionary<string, CallState> _calls = [];
    private CallState? _active;      // call owning the rewritable line
    private bool _lineOpen;          // an in-place (no newline yet) line is painted
    private bool _drafting;
    private string _draftName = "tool";
    private int _paintedWidth;
    private int _frame;
    private Timer? _timer;

    public ToolCallPresenter(
        object sync,
        IStyledWriter writer,
        Func<int> widthProvider,
        Func<string, string?>? serverLookup = null,
        bool interactive = true,
        bool enableTimer = true)
    {
        _sync = sync;
        _writer = writer;
        _widthProvider = widthProvider;
        _serverLookup = serverLookup;
        _interactive = interactive;
        _enableTimer = enableTimer;
    }

    /// <summary>True while a rewritable status line is painted (callers must interrupt before writing).</summary>
    public bool LineOpen => _lineOpen;

    // ─────────────────────────────── Lifecycle events ───────────────────────────────

    /// <summary>The model is streaming argument JSON for an upcoming call. Deltas themselves are not shown.</summary>
    public void OnDrafting(string? toolName)
    {
        if (!_interactive) return;

        string name = Displayable(toolName);
        if (_drafting && _lineOpen && name == _draftName) return;

        if (_lineOpen && !_drafting) CommitLine();
        _draftName = name;
        _drafting = true;
        PaintDraftLine();
        EnsureTimer();
    }

    /// <summary>The runtime is about to execute the tool.</summary>
    public void OnInvoked(FunctionCall call)
    {
        CallState state = new()
        {
            Display = Displayable(call.Name),
            ArgsSummary = ToolCallFormatter.SummarizeArguments(call.Arguments, ArgsBudget()),
            Watch = Stopwatch.StartNew(),
        };
        _calls[Key(call)] = state;

        if (!_interactive)
        {
            _writer.Write($"[tool] {state.Display}{state.ArgsSummary}", ArgsStyle);
            _writer.WriteLine();
            return;
        }

        if (_lineOpen && !_drafting) CommitLine();
        _drafting = false;
        _active = state;
        PaintToolLine(state, spinner: true);
        EnsureTimer();
    }

    /// <summary>The tool finished; show the result preview and duration.</summary>
    public void OnCompleted(FunctionCall call, FunctionResult? result)
    {
        string key = Key(call);
        _calls.Remove(key, out CallState? state);
        TimeSpan? elapsed = state?.Watch?.Elapsed;

        bool failed = result?.InvocationSucceeded == false;
        string preview = result is null ? "done" : ToolCallFormatter.PreviewResult(result, ResultBudget());
        string suffix = elapsed is { } t ? $" ({ToolCallFormatter.FormatDuration(t)})" : "";

        // Was the open line this very call's? Then the result attaches to it visually and needs no name tag.
        bool wasActiveLine = _interactive && _lineOpen && !_drafting && ReferenceEquals(_active, state);

        if (_interactive && _lineOpen)
        {
            if (_drafting)
            {
                ClearLine(); // transient draft lines are erased, not kept
                _drafting = false;
            }
            else
            {
                CommitLine(); // freeze "⏺ name(args)" — this call's line or another still-running tool's
            }
        }

        if (!wasActiveLine && state is not null)
        {
            preview = $"{state.Display}: {preview}";
        }

        string gutter = _interactive ? "  ⎿ " : "  -> ";
        _writer.Write(gutter, failed ? ErrorStyle : ResultStyle);
        if (failed) _writer.Write(_interactive ? "✗ " : "FAILED ", ErrorStyle);
        _writer.Write(preview + suffix, failed ? ErrorStyle : ResultStyle);
        _writer.WriteLine();

        if (_active is null || ReferenceEquals(_active, state))
        {
            _active = null;
            StopTimerIfIdle();
        }
    }

    /// <summary>
    /// Commits any in-place status line so other output (stream tokens, notices, prompts)
    /// can flow below it. Safe to call when nothing is open.
    /// </summary>
    public void InterruptForOutput()
    {
        if (!_lineOpen) return;
        if (_drafting)
        {
            ClearLine();
            _drafting = false;
        }
        else
        {
            CommitLine();
        }
        StopTimerIfIdle();
    }

    /// <summary>End of turn: commit or clear anything still on screen and reset call tracking.</summary>
    public void End()
    {
        InterruptForOutput();
        _calls.Clear();
        _active = null;
        StopTimer();
    }

    public void Dispose() => StopTimer();

    // ─────────────────────────────── Painting ───────────────────────────────

    private void Tick()
    {
        lock (_sync)
        {
            if (!_lineOpen) return;
            _frame++;
            if (_drafting) PaintDraftLine();
            else if (_active is not null) PaintToolLine(_active, spinner: true);
        }
    }

    private void PaintDraftLine()
    {
        Repaint([($"⚙ preparing {_draftName}… {SpinnerChar()}", DraftStyle)]);
    }

    private void PaintToolLine(CallState state, bool spinner)
    {
        List<(string, TextStyle)> segments =
        [
            ("⏺ ", DotStyle),
            (state.Display, NameStyle),
            (state.ArgsSummary, ArgsStyle),
        ];
        if (spinner && state.Watch is { } watch)
        {
            segments.Add(($"  {SpinnerChar()} {ToolCallFormatter.FormatDuration(watch.Elapsed)}", SpinStyle));
        }
        Repaint(segments);
    }

    private char SpinnerChar() => SpinnerFrames[_frame % SpinnerFrames.Length];

    /// <summary>Rewrites the current physical line in place, truncated to the terminal width.</summary>
    private void Repaint(List<(string Text, TextStyle Style)> segments)
    {
        int budget = Math.Max(4, _widthProvider() - 1);
        _writer.Write("\r", TextStyle.Default);

        int written = 0;
        foreach ((string text, TextStyle style) in segments)
        {
            if (written >= budget) break;
            string piece = DisplayWidth.TruncateToWidth(text, budget - written);
            _writer.Write(piece, style);
            written += DisplayWidth.Measure(piece);
        }

        if (written < _paintedWidth)
        {
            // Erase the tail of the previous, longer paint. The cursor parks at column 0; the
            // next repaint or the commit newline both start from there safely.
            _writer.Write(new string(' ', _paintedWidth - written), TextStyle.Default);
            _writer.Write("\r", TextStyle.Default);
        }
        _paintedWidth = written;
        _lineOpen = true;
    }

    /// <summary>Finalizes the open line: repaint without the spinner suffix, then newline.</summary>
    private void CommitLine()
    {
        if (!_lineOpen) return;
        if (_active is not null && !_drafting)
        {
            PaintToolLine(_active, spinner: false);
        }
        _writer.WriteLine();
        _lineOpen = false;
        _paintedWidth = 0;
        _active = null;
    }

    /// <summary>Erases the open line entirely (used for transient draft lines).</summary>
    private void ClearLine()
    {
        if (!_lineOpen) return;
        _writer.Write("\r", TextStyle.Default);
        _writer.Write(new string(' ', _paintedWidth), TextStyle.Default);
        _writer.Write("\r", TextStyle.Default);
        _lineOpen = false;
        _paintedWidth = 0;
    }

    // ─────────────────────────────── Helpers ───────────────────────────────

    private void EnsureTimer()
    {
        if (!_enableTimer || !_interactive) return;
        _timer ??= new Timer(_ => { try { Tick(); } catch { /* never crash on a paint */ } }, null, 100, 90);
    }

    private void StopTimerIfIdle()
    {
        if (!_lineOpen && _active is null) StopTimer();
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private string Displayable(string? toolName)
    {
        string name = string.IsNullOrWhiteSpace(toolName) ? "tool" : toolName;
        string? server = _serverLookup?.Invoke(name);
        return server is null ? name : $"{server}:{name}";
    }

    private static string Key(FunctionCall call) => call.ToolCall?.Id ?? call.Name;

    private int ArgsBudget() => Math.Max(10, _widthProvider() - 20);

    private int ResultBudget() => Math.Max(10, _widthProvider() - 16);
}
