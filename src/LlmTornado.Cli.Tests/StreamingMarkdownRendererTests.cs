using System.Text;
using LlmTornado.Cli.Rendering;

namespace LlmTornado.Cli.Tests;

/// <summary>Captures styled spans for assertions; merges adjacent same-style writes.</summary>
internal sealed class RecordingWriter : IStyledWriter
{
    public readonly List<(string Text, TextStyle Style)> Spans = [];

    public void Write(string text, TextStyle style)
    {
        if (text.Length == 0) return;
        if (Spans.Count > 0 && Spans[^1].Style == style)
        {
            Spans[^1] = (Spans[^1].Text + text, style);
        }
        else
        {
            Spans.Add((text, style));
        }
    }

    public void WriteLine() => Write("\n", new TextStyle(StyleFlags.None));

    public void Reset()
    {
    }

    /// <summary>Raw text with style boundaries dropped — for content-only assertions.</summary>
    public string Text => string.Concat(Spans.Select(s => s.Text));

    /// <summary>Canonical dump: one "text|flags|fg" entry per span, for exact comparisons.</summary>
    public string Dump()
    {
        StringBuilder sb = new();
        foreach ((string text, TextStyle style) in Spans)
        {
            sb.Append(text.Replace("\n", "\\n")).Append('|').Append(style.Flags).Append('|').Append(style.Fg?.ToString() ?? "-").Append(';');
        }
        return sb.ToString();
    }
}

[TestFixture]
public class StreamingMarkdownRendererTests
{
    private const int Width = 41; // usable width = 40

    private static (RecordingWriter Writer, StreamingMarkdownRenderer Renderer) Create(int width = Width)
    {
        RecordingWriter writer = new();
        StreamingMarkdownRenderer renderer = new(writer, MarkdownTheme.Output, () => width);
        return (writer, renderer);
    }

    private static RecordingWriter Render(string markdown, int width = Width)
    {
        (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create(width);
        renderer.Push(markdown);
        renderer.Flush();
        return writer;
    }

    // ─── Chunk-boundary invariance: the core streaming property ───

    private static readonly string[] Documents =
    [
        "Hello **bold** and *italic* and `code` here.\n",
        "# Title\nBody text under the header.\n",
        "## Sub *header* line\ntext\n",
        "- first item\n- second **bold** item\n  - nested\n",
        "1. one\n2. two\n10) ten\n",
        "> quoted text with **emphasis**\n> second line\n",
        "```python\ndef f(x):\n    return x * 2\n```\nafter fence\n",
        "```\nplain fence\n```\n",
        "before\n---\nafter\n",
        "A paragraph that is long enough to wrap across multiple physical lines at forty columns easily.\n",
        "intra_word_underscores stay literal\n",
        "_leading italic_ and trailing\n",
        "mixed ***bold italic*** text\n",
        "日本語のテキストが折り返される場合のテストです。これは長い行です。\n",
        "emoji 🎉 in **bold 🚀 text** works\n",
        "text with `inline **not bold** code` end\n",
        "dangling **opener at line end\nnext line\n",
        "``` \nfence with trailing space lang\n```\n",
    ];

    [Test]
    public void ChunkBoundaryInvariance_CharByChar_MatchesWholeChunk()
    {
        foreach (string doc in Documents)
        {
            RecordingWriter whole = Render(doc);

            (RecordingWriter chars, StreamingMarkdownRenderer renderer) = Create();
            foreach (char c in doc) renderer.Push(c.ToString());
            renderer.Flush();

            Assert.That(chars.Dump(), Is.EqualTo(whole.Dump()), $"char-by-char mismatch for: {doc[..Math.Min(40, doc.Length)]}");
        }
    }

    [Test]
    public void ChunkBoundaryInvariance_EverySplitPoint_MatchesWholeChunk()
    {
        foreach (string doc in Documents)
        {
            string expected = Render(doc).Dump();

            for (int split = 1; split < doc.Length; split++)
            {
                (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create();
                renderer.Push(doc[..split]);
                renderer.Push(doc[split..]);
                renderer.Flush();

                Assert.That(writer.Dump(), Is.EqualTo(expected), $"split at {split} mismatch for: {doc[..Math.Min(40, doc.Length)]}");
            }
        }
    }

    // ─── Inline styling ───

    [Test]
    public void Bold_TogglesStyle_AndDropsMarkers()
    {
        RecordingWriter writer = Render("a **b** c\n");
        Assert.That(writer.Text, Is.EqualTo("a b c\n"));
        (string text, TextStyle style) = writer.Spans.Single(s => s.Text.Contains('b'));
        Assert.That(style.Flags.HasFlag(StyleFlags.Bold), Is.True, $"span '{text}' should be bold");
    }

    [Test]
    public void InlineCode_SuppressesNestedEmphasis()
    {
        RecordingWriter writer = Render("x `**lit**` y\n");
        Assert.That(writer.Text, Is.EqualTo("x **lit** y\n"));
        Assert.That(writer.Spans.Any(s => s.Text.Contains("**lit**") && s.Style.Fg == ConsoleColor.Yellow), Is.True);
    }

    [Test]
    public void IntrawordUnderscore_StaysLiteral()
    {
        RecordingWriter writer = Render("snake_case_name\n");
        Assert.That(writer.Text, Is.EqualTo("snake_case_name\n"));
    }

    [Test]
    public void DanglingOpener_AtLineEnd_IsLiteral()
    {
        RecordingWriter writer = Render("text **\nnext\n");
        Assert.That(writer.Text, Is.EqualTo("text **\nnext\n"));
    }

    [Test]
    public void Flush_EmitsDanglingDelimiterLiterally()
    {
        (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create();
        renderer.Push("tail *");
        renderer.Flush();
        Assert.That(writer.Text, Is.EqualTo("tail *"));
    }

    // ─── Blocks ───

    [Test]
    public void Header_StripsMarker_AppliesHeaderStyle()
    {
        RecordingWriter writer = Render("# Big Title\n");
        Assert.That(writer.Text, Is.EqualTo("Big Title\n"));
        Assert.That(writer.Spans.First(s => s.Text.Contains("Big")).Style.Flags.HasFlag(StyleFlags.Bold), Is.True);
    }

    [Test]
    public void Header_AfterText_GetsBlankLineSeparator()
    {
        RecordingWriter writer = Render("para\n# Head\n");
        Assert.That(writer.Text, Is.EqualTo("para\n\nHead\n"));
    }

    [Test]
    public void HashWithoutSpace_IsNotAHeader()
    {
        RecordingWriter writer = Render("#hashtag\n");
        Assert.That(writer.Text, Is.EqualTo("#hashtag\n"));
    }

    [Test]
    public void Bullet_RendersDotMarker()
    {
        RecordingWriter writer = Render("- item one\n");
        Assert.That(writer.Text, Is.EqualTo("• item one\n"));
    }

    [Test]
    public void NestedBullet_PreservesIndent()
    {
        RecordingWriter writer = Render("- a\n  - b\n");
        Assert.That(writer.Text, Is.EqualTo("• a\n  • b\n"));
    }

    [Test]
    public void OrderedList_KeepsNumbers()
    {
        RecordingWriter writer = Render("1. one\n12. twelve\n");
        Assert.That(writer.Text, Is.EqualTo("1. one\n12. twelve\n"));
    }

    [Test]
    public void Quote_GetsGutter()
    {
        RecordingWriter writer = Render("> hello\n");
        Assert.That(writer.Text, Is.EqualTo("│ hello\n"));
    }

    [Test]
    public void HorizontalRule_RendersLine()
    {
        RecordingWriter writer = Render("---\n");
        Assert.That(writer.Text.TrimEnd('\n'), Does.Match("^─+$"));
    }

    [Test]
    public void ThreeDashes_WithTrailingText_IsParagraph()
    {
        RecordingWriter writer = Render("---x\n");
        Assert.That(writer.Text, Is.EqualTo("---x\n"));
    }

    // ─── Code fences ───

    [Test]
    public void Fence_RendersGutterAndLangLabel_NoInlineParsing()
    {
        RecordingWriter writer = Render("```cs\nvar x = a * b; // **not bold**\n```\n");
        string[] lines = writer.Text.Split('\n');
        Assert.That(lines[0], Does.StartWith("╭─"));
        Assert.That(lines[0], Does.Contain("cs"));
        Assert.That(lines[1], Is.EqualTo("│ var x = a * b; // **not bold**"));
        Assert.That(lines[2], Does.StartWith("╰─"));
    }

    [Test]
    public void Fence_BlankLineInside_KeepsGutter()
    {
        RecordingWriter writer = Render("```\na\n\nb\n```\n");
        Assert.That(writer.Text, Is.EqualTo("╭─\n│ a\n│ \n│ b\n╰─\n"));
    }

    [Test]
    public void Fence_SplitMidMarker_StillOpens()
    {
        (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create();
        renderer.Push("`");
        renderer.Push("``py");
        renderer.Push("thon\ncode\n``");
        renderer.Push("`\n");
        renderer.Flush();
        Assert.That(writer.Text, Is.EqualTo("╭─ python\n│ code\n╰─\n"));
    }

    // ─── Wrapping ───

    [Test]
    public void LongParagraph_WrapsAtWordBoundaries()
    {
        RecordingWriter writer = Render("aaa bbb ccc ddd\n", width: 9); // usable = 8
        Assert.That(writer.Text, Is.EqualTo("aaa bbb\nccc ddd\n"));
    }

    [Test]
    public void WrappedBullet_GetsHangingIndent()
    {
        RecordingWriter writer = Render("- aaa bbb ccc\n", width: 9); // usable = 8, content width 6
        string[] lines = writer.Text.TrimEnd('\n').Split('\n');
        Assert.That(lines[0], Is.EqualTo("• aaa"));
        Assert.That(lines[1], Does.StartWith("  "));
    }

    [Test]
    public void WideChars_WrapByDisplayWidth()
    {
        // Each ideograph is 2 columns; usable width 10 fits 5 per line when unbroken by spaces.
        RecordingWriter writer = Render("日本語日本語日本\n", width: 11);
        foreach (string line in writer.Text.TrimEnd('\n').Split('\n'))
        {
            Assert.That(DisplayWidth.Measure(line), Is.LessThanOrEqualTo(10));
        }
    }

    [Test]
    public void AtLineStart_TracksPartialLines()
    {
        (RecordingWriter _, StreamingMarkdownRenderer renderer) = Create();
        Assert.That(renderer.AtLineStart, Is.True);
        renderer.Push("partial");
        Assert.That(renderer.AtLineStart, Is.False);
        renderer.Push(" word\n");
        Assert.That(renderer.AtLineStart, Is.True);
    }

    [Test]
    public void CompleteLine_EndsPartialLine()
    {
        (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create();
        renderer.Push("partial");
        renderer.CompleteLine();
        Assert.That(writer.Text, Is.EqualTo("partial\n"));
        Assert.That(renderer.AtLineStart, Is.True);
    }

    [Test]
    public void Reset_ClearsFenceState()
    {
        (RecordingWriter writer, StreamingMarkdownRenderer renderer) = Create();
        renderer.Push("```\ncode");
        renderer.Reset();
        renderer.Push("plain\n");
        Assert.That(writer.Text, Does.EndWith("plain\n"));
        Assert.That(writer.Text, Does.Not.EndWith("│ plain\n"));
    }
}
