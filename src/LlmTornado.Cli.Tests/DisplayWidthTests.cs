using LlmTornado.Cli.Rendering;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class DisplayWidthTests
{
    [TestCase("", 0)]
    [TestCase("hello", 5)]
    [TestCase("hello world", 11)]
    public void Measure_Ascii(string text, int expected)
    {
        Assert.That(DisplayWidth.Measure(text), Is.EqualTo(expected));
    }

    [Test]
    public void Measure_Cjk_CountsTwoColumnsPerIdeograph()
    {
        Assert.That(DisplayWidth.Measure("日本語"), Is.EqualTo(6));
        Assert.That(DisplayWidth.Measure("한글"), Is.EqualTo(4));
        Assert.That(DisplayWidth.Measure("中a文"), Is.EqualTo(5));
    }

    [Test]
    public void Measure_Emoji_CountsTwoColumns()
    {
        Assert.That(DisplayWidth.Measure("🎉"), Is.EqualTo(2));   // surrogate pair, U+1F389
        Assert.That(DisplayWidth.Measure("a🚀b"), Is.EqualTo(4));
    }

    [Test]
    public void Measure_CombiningMarks_AreZeroWidth()
    {
        // "e" + U+0301 combining acute accent
        Assert.That(DisplayWidth.Measure("é"), Is.EqualTo(1));
    }

    [Test]
    public void Measure_ZeroWidthJoinerAndVariationSelector_AreZeroWidth()
    {
        Assert.That(DisplayWidth.Measure("‍"), Is.EqualTo(0));
        Assert.That(DisplayWidth.Measure("️"), Is.EqualTo(0));
    }

    [Test]
    public void Measure_FullwidthForms_AreWide()
    {
        Assert.That(DisplayWidth.Measure("ＡＢ"), Is.EqualTo(4)); // U+FF21 U+FF22
    }

    [Test]
    public void Measure_LoneSurrogate_CountsOneColumn()
    {
        Assert.That(DisplayWidth.Measure("\uD83D"), Is.EqualTo(1));
    }

    [Test]
    public void TruncateToWidth_Ascii()
    {
        Assert.That(DisplayWidth.TruncateToWidth("hello", 3), Is.EqualTo("hel"));
        Assert.That(DisplayWidth.TruncateToWidth("hello", 10), Is.EqualTo("hello"));
        Assert.That(DisplayWidth.TruncateToWidth("hello", 0), Is.EqualTo(""));
    }

    [Test]
    public void TruncateToWidth_DoesNotSplitWideRune()
    {
        // "日" is 2 columns; a 3-column budget fits "日" (2) but not "日本" (4).
        Assert.That(DisplayWidth.TruncateToWidth("日本", 3), Is.EqualTo("日"));
    }

    [Test]
    public void TruncateToWidth_DoesNotSplitSurrogatePair()
    {
        Assert.That(DisplayWidth.TruncateToWidth("🎉🎉", 3), Is.EqualTo("🎉"));
    }
}
