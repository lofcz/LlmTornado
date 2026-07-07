using LlmTornado.ChatFunctions;
using LlmTornado.Cli.Rendering;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ToolCallFormatterTests
{
    [Test]
    public void SummarizeArguments_SimpleObject_RendersPairs()
    {
        string summary = ToolCallFormatter.SummarizeArguments("{\"path\":\"src/foo.cs\",\"lines\":10}", 80);
        Assert.That(summary, Is.EqualTo("(path: \"src/foo.cs\", lines: 10)"));
    }

    [Test]
    public void SummarizeArguments_CapsAtThreePairs()
    {
        string summary = ToolCallFormatter.SummarizeArguments("{\"a\":1,\"b\":2,\"c\":3,\"d\":4}", 80);
        Assert.That(summary, Is.EqualTo("(a: 1, b: 2, c: 3, …)"));
    }

    [Test]
    public void SummarizeArguments_NestedValues_Elided()
    {
        string summary = ToolCallFormatter.SummarizeArguments("{\"filter\":{\"x\":1},\"ids\":[1,2]}", 80);
        Assert.That(summary, Is.EqualTo("(filter: {…}, ids: […])"));
    }

    [Test]
    public void SummarizeArguments_LongString_Truncated()
    {
        string summary = ToolCallFormatter.SummarizeArguments($"{{\"text\":\"{new string('x', 200)}\"}}", 200);
        Assert.That(summary, Does.Contain("…"));
        Assert.That(summary.Length, Is.LessThan(60));
    }

    [Test]
    public void SummarizeArguments_MalformedJson_DegradesGracefully()
    {
        Assert.That(ToolCallFormatter.SummarizeArguments("{\"partial\":", 80), Is.EqualTo("(…)"));
    }

    [Test]
    public void SummarizeArguments_Empty_IsEmptyParens()
    {
        Assert.That(ToolCallFormatter.SummarizeArguments(null, 80), Is.EqualTo("()"));
        Assert.That(ToolCallFormatter.SummarizeArguments("", 80), Is.EqualTo("()"));
    }

    [Test]
    public void SummarizeArguments_ClampsToWidth()
    {
        string summary = ToolCallFormatter.SummarizeArguments("{\"path\":\"a-fairly-long-path/to/somewhere.cs\"}", 20);
        Assert.That(DisplayWidth.Measure(summary), Is.LessThanOrEqualTo(21));
        Assert.That(summary, Does.EndWith("…)"));
    }

    [Test]
    public void PreviewResult_CollapsesWhitespace_AndTruncates()
    {
        FunctionResult result = new("t", "line one\n   line two\n\nline three", FunctionResultSetContentModes.Passthrough);
        string preview = ToolCallFormatter.PreviewResult(result, 80);
        Assert.That(preview, Is.EqualTo("line one line two line three"));
    }

    [Test]
    public void PreviewResult_Empty_SaysDone()
    {
        FunctionResult result = new("t", "", FunctionResultSetContentModes.Passthrough);
        Assert.That(ToolCallFormatter.PreviewResult(result, 80), Is.EqualTo("done"));
    }

    [Test]
    public void PreviewResult_FailedEmpty_SaysFailed()
    {
        FunctionResult result = new("t", "", FunctionResultSetContentModes.Passthrough, invocationSucceeded: false);
        Assert.That(ToolCallFormatter.PreviewResult(result, 80), Is.EqualTo("failed"));
    }

    [TestCase(0.42, "0.4s")]
    [TestCase(9.94, "9.9s")]
    [TestCase(42.6, "43s")]
    [TestCase(95.0, "1m 35s")]
    public void FormatDuration_Formats(double seconds, string expected)
    {
        Assert.That(ToolCallFormatter.FormatDuration(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expected));
    }
}

[TestFixture]
public class ToolCallPresenterTests
{
    private static FunctionCall Call(string name, string args = "{}", string? id = null)
    {
        FunctionCall call = new() { Name = name, Arguments = args };
        if (id is not null)
        {
            call.ToolCall = new ToolCall { Id = id };
        }
        return call;
    }

    private static (RecordingWriter Writer, ToolCallPresenter Presenter) Create(bool interactive)
    {
        RecordingWriter writer = new();
        ToolCallPresenter presenter = new(
            sync: new object(),
            writer: writer,
            widthProvider: () => 100,
            serverLookup: name => name.StartsWith("mcp_") ? "ctx7" : null,
            interactive: interactive,
            enableTimer: false);
        return (writer, presenter);
    }

    [Test]
    public void NonInteractive_InvokedAndCompleted_PrintStaticLines()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: false);

        presenter.OnInvoked(Call("read_file", "{\"path\":\"a.cs\"}"));
        presenter.OnCompleted(Call("read_file"), new FunctionResult("read_file", "ok", FunctionResultSetContentModes.Passthrough));

        Assert.That(writer.Text, Does.Contain("[tool] read_file(path: \"a.cs\")\n"));
        Assert.That(writer.Text, Does.Contain("-> read_file: ok ("));
    }

    [Test]
    public void NonInteractive_Drafting_PrintsNothing()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: false);
        presenter.OnDrafting("read_file");
        Assert.That(writer.Text, Is.Empty);
    }

    [Test]
    public void Interactive_HappyPath_ToolLineThenResultLine()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);

        presenter.OnDrafting("read_file");
        presenter.OnInvoked(Call("read_file", "{\"path\":\"a.cs\"}", id: "c1"));
        presenter.OnCompleted(Call("read_file", id: "c1"), new FunctionResult("read_file", "42 lines", FunctionResultSetContentModes.Passthrough));

        string text = writer.Text;
        Assert.That(text, Does.Contain("⚙ preparing read_file…"));
        Assert.That(text, Does.Contain("⏺ read_file(path: \"a.cs\")"));
        Assert.That(text, Does.Contain("⎿ 42 lines ("));
        // Result line must come after the committed tool line's newline.
        Assert.That(text.IndexOf("⎿"), Is.GreaterThan(text.IndexOf("⏺ read_file")));
        Assert.That(presenter.LineOpen, Is.False);
    }

    [Test]
    public void Interactive_FailedResult_MarksError()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);

        presenter.OnInvoked(Call("boom", id: "c1"));
        presenter.OnCompleted(Call("boom", id: "c1"), new FunctionResult("boom", "kaput", FunctionResultSetContentModes.Passthrough, invocationSucceeded: false));

        Assert.That(writer.Text, Does.Contain("✗"));
        Assert.That(writer.Spans.Any(s => s.Text.Contains("kaput") && s.Style.Fg == ConsoleColor.Red), Is.True);
    }

    [Test]
    public void Interactive_ParallelCalls_OutOfOrderCompletion_TagsResultWithName()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);

        presenter.OnInvoked(Call("alpha", id: "a"));
        presenter.OnInvoked(Call("beta", id: "b"));  // commits alpha's line, beta becomes active
        presenter.OnCompleted(Call("alpha", id: "a"), new FunctionResult("alpha", "A-done", FunctionResultSetContentModes.Passthrough));
        presenter.OnCompleted(Call("beta", id: "b"), new FunctionResult("beta", "B-done", FunctionResultSetContentModes.Passthrough));

        string text = writer.Text;
        Assert.That(text, Does.Contain("⎿ alpha: A-done"));   // alpha's line was no longer active → tagged
        Assert.That(text, Does.Contain("⎿ beta: B-done"));    // beta's line was interrupted by alpha's result → tagged
    }

    [Test]
    public void InterruptForOutput_CommitsOpenLine()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);

        presenter.OnInvoked(Call("slow", id: "c1"));
        Assert.That(presenter.LineOpen, Is.True);

        presenter.InterruptForOutput();
        Assert.That(presenter.LineOpen, Is.False);
        Assert.That(writer.Text, Does.EndWith("\n"));
    }

    [Test]
    public void InterruptForOutput_ClearsDraftLine()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);

        presenter.OnDrafting("pending_tool");
        presenter.InterruptForOutput();

        Assert.That(presenter.LineOpen, Is.False);
        // Draft line is transient: it is erased, not committed with a newline.
        Assert.That(writer.Text, Does.Not.EndWith("\n"));
    }

    [Test]
    public void McpTool_GetsServerBadge()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);
        presenter.OnInvoked(Call("mcp_query", id: "c1"));
        Assert.That(writer.Text, Does.Contain("ctx7:mcp_query"));
    }

    [Test]
    public void CompletionWithoutInvoke_StillPrintsResult()
    {
        (RecordingWriter writer, ToolCallPresenter presenter) = Create(interactive: true);
        presenter.OnCompleted(Call("ghost"), new FunctionResult("ghost", "ok", FunctionResultSetContentModes.Passthrough));
        Assert.That(writer.Text, Does.Contain("⎿ ok"));
    }
}
