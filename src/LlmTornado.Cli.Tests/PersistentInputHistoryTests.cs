using LlmTornado.Cli.Input;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class PersistentInputHistoryTests
{
    private string _tempDir = null!;
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tornado-history-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "input-history.txt");
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Test]
    public void Load_MissingFile_ReturnsEmpty()
    {
        Assert.That(PersistentInputHistory.Load(_path), Is.Empty);
    }

    [Test]
    public void AppendAndLoad_RoundTrips()
    {
        PersistentInputHistory.Append(_path, "/help");
        PersistentInputHistory.Append(_path, "how do I frobnicate?");

        List<string> loaded = PersistentInputHistory.Load(_path);
        Assert.That(loaded, Is.EqualTo(new[] { "/help", "how do I frobnicate?" }));
    }

    [Test]
    public void MultilineEntry_SurvivesAsOneEntry()
    {
        string entry = "first line\nsecond line\r\nthird";
        PersistentInputHistory.Append(_path, entry);

        List<string> loaded = PersistentInputHistory.Load(_path);
        Assert.That(loaded, Has.Count.EqualTo(1));
        Assert.That(loaded[0], Is.EqualTo(entry));
    }

    [Test]
    public void BackslashContent_RoundTrips()
    {
        string entry = @"read C:\Users\me\file.txt and explain \n literally";
        PersistentInputHistory.Append(_path, entry);
        Assert.That(PersistentInputHistory.Load(_path).Single(), Is.EqualTo(entry));
    }

    [Test]
    public void Load_TrimsToMaxEntries_KeepingNewest()
    {
        for (int i = 0; i < 30; i++)
            PersistentInputHistory.Append(_path, $"entry {i}");

        List<string> loaded = PersistentInputHistory.Load(_path, maxEntries: 10);
        Assert.That(loaded, Has.Count.EqualTo(10));
        Assert.That(loaded[0], Is.EqualTo("entry 20"));
        Assert.That(loaded[^1], Is.EqualTo("entry 29"));
    }

    [Test]
    public void Load_CorruptFile_DegradesToUsableEntries()
    {
        File.WriteAllText(_path, "good entry\n\n\ttrailing tab entry\t\n");
        List<string> loaded = PersistentInputHistory.Load(_path);
        Assert.That(loaded, Is.EqualTo(new[] { "good entry", "trailing tab entry" }));
    }

    [Test]
    public void Append_BlankEntry_IsIgnored()
    {
        PersistentInputHistory.Append(_path, "   ");
        Assert.That(File.Exists(_path), Is.False);
    }
}

[TestFixture]
public class CommandHistoryNavigatorSeedTests
{
    [Test]
    public void Seed_IsNavigable()
    {
        CommandHistoryNavigator nav = new(["one", "two"]);
        Assert.That(nav.TryMovePrevious("", out string recalled), Is.True);
        Assert.That(recalled, Is.EqualTo("two"));
        Assert.That(nav.TryMovePrevious(recalled, out recalled), Is.True);
        Assert.That(recalled, Is.EqualTo("one"));
    }

    [Test]
    public void Seed_SkipsBlanksAndConsecutiveDuplicates()
    {
        CommandHistoryNavigator nav = new(["a", "a", "", "b"]);
        Assert.That(nav.Entries, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void Seed_TrimsToMaxEntries()
    {
        CommandHistoryNavigator nav = new(Enumerable.Range(0, 50).Select(i => $"e{i}"), maxEntries: 5);
        Assert.That(nav.Entries, Is.EqualTo(new[] { "e45", "e46", "e47", "e48", "e49" }));
    }

    [Test]
    public void EntryAdded_FiresForNewEntries_NotDuplicates()
    {
        CommandHistoryNavigator nav = new();
        List<string> sink = [];
        nav.EntryAdded += sink.Add;

        nav.AddSubmitted("hello");
        nav.AddSubmitted("hello"); // consecutive duplicate
        nav.AddSubmitted("world");
        nav.AddSubmitted("  ");    // blank

        Assert.That(sink, Is.EqualTo(new[] { "hello", "world" }));
    }
}
