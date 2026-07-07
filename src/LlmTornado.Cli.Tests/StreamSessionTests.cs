using LlmTornado.ChatFunctions;
using LlmTornado.Cli.Rendering;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class StreamSessionTests
{
    private static (RecordingWriter Writer, StreamSession Session) Create()
    {
        RecordingWriter writer = new();
        ToolCallPresenter presenter = new(
            sync: new object(),
            writer: writer,
            widthProvider: () => 100,
            interactive: true,
            enableTimer: false);
        StreamSession session = new(writer, presenter, () => 100);
        return (writer, session);
    }

    [Test]
    public void ReasoningThenOutput_GetsLabelAndSeparator()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushReasoning("pondering deeply\n");
        session.PushOutput("The answer.\n");
        session.End();

        string text = writer.Text;
        Assert.That(text, Does.StartWith("· thinking\n"));
        Assert.That(text, Does.Contain("pondering deeply\n\nThe answer.\n"));
    }

    [Test]
    public void ThinkingLabel_ShownOnlyOncePerTurn()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushReasoning("a\n");
        session.PushOutput("b\n");
        session.PushReasoning("c\n");
        session.End();

        int count = writer.Text.Split("· thinking").Length - 1;
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Notice_MidParagraph_LandsOnOwnLine()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushOutput("streaming without newline");
        session.BeginNotice();
        writer.Write("[notice]", TextStyle.Default);
        writer.WriteLine();
        session.PushOutput("resumed text\n");
        session.End();

        Assert.That(writer.Text, Does.Contain("streaming without newline\n[notice]\nresumed text\n"));
    }

    [Test]
    public void ToolCall_MidOutput_CommittedAndSeparated()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushOutput("Let me check.");
        FunctionCall call = new() { Name = "probe", Arguments = "{}" };
        session.ToolInvoked(call);
        session.ToolCompleted(call, new FunctionResult("probe", "found it", FunctionResultSetContentModes.Passthrough));
        session.PushOutput("Done checking.\n");
        session.End();

        string text = writer.Text;
        Assert.That(text, Does.Contain("Let me check.\n"));
        Assert.That(text, Does.Contain("⏺ probe()"));
        Assert.That(text, Does.Contain("⎿ found it"));
        // Output resumes after a blank separator following the tool block.
        Assert.That(text.IndexOf("Done checking."), Is.GreaterThan(text.IndexOf("⎿ found it")));
    }

    [Test]
    public void End_ResetsMarkdownState_AcrossTurns()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushOutput("```\nunclosed fence");
        session.End();
        session.PushOutput("fresh paragraph\n");
        session.End();

        Assert.That(writer.Text, Does.Contain("\nfresh paragraph\n"));
        Assert.That(writer.Text, Does.Not.Contain("│ fresh paragraph"));
    }

    [Test]
    public void End_WhenIdle_IsSafe()
    {
        (RecordingWriter writer, StreamSession session) = Create();
        Assert.DoesNotThrow(session.End);
        Assert.That(writer.Text, Is.Empty);
    }

    [Test]
    public void Markdown_IsStyled_InOutputPhase()
    {
        (RecordingWriter writer, StreamSession session) = Create();

        session.PushOutput("**important**\n");
        session.End();

        Assert.That(writer.Text, Does.Contain("important"));
        Assert.That(writer.Text, Does.Not.Contain("**"));
        Assert.That(writer.Spans.Any(s => s.Text.Contains("important") && s.Style.Flags.HasFlag(StyleFlags.Bold)), Is.True);
    }
}
