using System.Text;
using LlmTornado.Cli.Core.Input;
using LlmTornado.Cli.Input;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class AutocompleteTests
{
    #region DetectCompletion

    [Test]
    public void Detect_Slash_At_Start_Is_Command()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("/", 1);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.Command));
        Assert.That(ctx.Partial, Is.EqualTo(""));
        Assert.That(ctx.TokenStart, Is.EqualTo(0));
        Assert.That(ctx.TokenEnd, Is.EqualTo(1));
    }

    [Test]
    public void Detect_Partial_Command()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("/he", 3);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.Command));
        Assert.That(ctx.Partial, Is.EqualTo("he"));
    }

    [Test]
    public void Detect_Command_Honors_Leading_Whitespace()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("  /mo", 5);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.Command));
        Assert.That(ctx.Partial, Is.EqualTo("mo"));
        Assert.That(ctx.TokenStart, Is.EqualTo(2));
    }

    [Test]
    public void Detect_No_Command_When_Cursor_In_Argument()
    {
        // Cursor sits in the second token ("li"), not the command word.
        CompletionContext ctx = LineEditor.DetectCompletion("/model li", 9);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.None));
    }

    [Test]
    public void Detect_Slash_Mid_Sentence_Is_Not_Command()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("what is /help", 13);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.None));
    }

    [Test]
    public void Detect_At_Token_Is_File()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("see @sr", 7);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.File));
        Assert.That(ctx.Partial, Is.EqualTo("sr"));
        Assert.That(ctx.TokenStart, Is.EqualTo(4));
        Assert.That(ctx.TokenEnd, Is.EqualTo(7));
    }

    [Test]
    public void Detect_At_With_Path_Partial()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("@src/fo", 7);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.File));
        Assert.That(ctx.Partial, Is.EqualTo("src/fo"));
    }

    [Test]
    public void Detect_At_Strips_Leading_Quote()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("@\"my", 4);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.File));
        Assert.That(ctx.Partial, Is.EqualTo("my"));
    }

    [Test]
    public void Detect_Plain_Text_Is_None()
    {
        CompletionContext ctx = LineEditor.DetectCompletion("hello world", 5);
        Assert.That(ctx.Kind, Is.EqualTo(CompletionKind.None));
    }

    #endregion

    #region ApplyCompletion

    [Test]
    public void Apply_Command_Replaces_Token_With_Trailing_Space()
    {
        StringBuilder buffer = new("/he");
        CompletionContext ctx = LineEditor.DetectCompletion("/he", 3);
        int cursor = LineEditor.ApplyCompletion(buffer, ctx, "/help");
        Assert.That(buffer.ToString(), Is.EqualTo("/help "));
        Assert.That(cursor, Is.EqualTo(6));
    }

    [Test]
    public void Apply_File_Inserts_At_Prefix()
    {
        StringBuilder buffer = new("see @sr");
        CompletionContext ctx = LineEditor.DetectCompletion("see @sr", 7);
        int cursor = LineEditor.ApplyCompletion(buffer, ctx, "src/foo.png");
        Assert.That(buffer.ToString(), Is.EqualTo("see @src/foo.png "));
        Assert.That(cursor, Is.EqualTo(buffer.Length));
    }

    [Test]
    public void Apply_File_Quotes_Path_With_Spaces()
    {
        StringBuilder buffer = new("@my");
        CompletionContext ctx = LineEditor.DetectCompletion("@my", 3);
        LineEditor.ApplyCompletion(buffer, ctx, "my doc.pdf");
        Assert.That(buffer.ToString(), Is.EqualTo("@\"my doc.pdf\" "));
    }

    #endregion

    #region InputViewport

    [Test]
    public void Viewport_Fits_Whole_Buffer_When_Within_Width()
    {
        InputViewport viewport = LineEditor.ComputeViewport("hello", cursor: 5, width: 10);
        Assert.That(viewport.StartIndex, Is.EqualTo(0));
        Assert.That(viewport.Text, Is.EqualTo("hello"));
        Assert.That(viewport.CursorColumn, Is.EqualTo(5));
    }

    [Test]
    public void Viewport_Shows_Tail_When_Cursor_At_End_And_Buffer_Overflows()
    {
        InputViewport viewport = LineEditor.ComputeViewport("abcdefghij", cursor: 10, width: 4);
        Assert.That(viewport.StartIndex, Is.EqualTo(6));
        Assert.That(viewport.Text, Is.EqualTo("ghij"));
        Assert.That(viewport.CursorColumn, Is.EqualTo(4));
    }

    [Test]
    public void Viewport_Keeps_Cursor_Visible_When_Editing_Middle_Of_Long_Buffer()
    {
        InputViewport viewport = LineEditor.ComputeViewport("abcdefghij", cursor: 6, width: 4);
        Assert.That(viewport.StartIndex, Is.EqualTo(3));
        Assert.That(viewport.Text, Is.EqualTo("defg"));
        Assert.That(viewport.CursorColumn, Is.EqualTo(3));
    }

    [Test]
    public void Viewport_Clamps_Cursor_Beyond_Buffer_Length()
    {
        InputViewport viewport = LineEditor.ComputeViewport("abc", cursor: 99, width: 2);
        Assert.That(viewport.StartIndex, Is.EqualTo(1));
        Assert.That(viewport.Text, Is.EqualTo("bc"));
        Assert.That(viewport.CursorColumn, Is.EqualTo(2));
    }

    [Test]
    public void Viewport_Zero_Width_Returns_Empty_Visible_Text()
    {
        InputViewport viewport = LineEditor.ComputeViewport("abcdef", cursor: 3, width: 0);
        Assert.That(viewport.StartIndex, Is.EqualTo(0));
        Assert.That(viewport.Text, Is.EqualTo(string.Empty));
        Assert.That(viewport.CursorColumn, Is.EqualTo(0));
    }

    #endregion

    #region FileSuggestionProvider

    [Test]
    public void ScanFiles_Returns_Only_Supported_Types_And_Skips_Ignored_Dirs()
    {
        string root = CreateTempTree();
        try
        {
            List<string> files = FileSuggestionProvider.ScanFiles(root);
            List<string> normalized = files.Select(f => f.Replace('\\', '/')).ToList();

            Assert.That(normalized, Does.Contain("a.png"));
            Assert.That(normalized, Does.Contain("sub/c.pdf"));
            Assert.That(normalized, Does.Not.Contain("b.txt"));        // unsupported extension
            Assert.That(normalized.Any(f => f.Contains("bin/")), Is.False); // ignored dir
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Rank_Filters_By_Partial_And_Prefers_Filename_Matches()
    {
        List<string> files =
        [
            "src/report.pdf",
            "docs/report-final.pdf",
            "images/logo.png",
        ];

        IReadOnlyList<string> ranked = FileSuggestionProvider.Rank(files, "report");
        Assert.That(ranked, Has.Count.EqualTo(2));
        Assert.That(ranked, Does.Not.Contain("images/logo.png"));
        // Filename-prefix match ("report.pdf") ranks ahead of the path-substring match.
        Assert.That(ranked[0], Is.EqualTo("src/report.pdf"));
    }

    [Test]
    public void Rank_Empty_Partial_Returns_All_Up_To_Max()
    {
        List<string> files = ["a.png", "b.pdf", "c.mp3"];
        IReadOnlyList<string> ranked = FileSuggestionProvider.Rank(files, "", max: 2);
        Assert.That(ranked, Has.Count.EqualTo(2));
    }

    private static string CreateTempTree()
    {
        string root = Path.Combine(Path.GetTempPath(), "llmt-autocomplete-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "a.png"), "x");
        File.WriteAllText(Path.Combine(root, "b.txt"), "x");

        string sub = Path.Combine(root, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "c.pdf"), "x");

        string bin = Path.Combine(root, "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "d.png"), "x");

        return root;
    }

    #endregion
}
