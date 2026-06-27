using LlmTornado.Cli.Input;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class CommandHistoryNavigatorTests
{
    [Test]
    public void AddSubmitted_Stores_Only_Slash_Commands()
    {
        CommandHistoryNavigator history = new();

        history.AddSubmitted("hello world");
        history.AddSubmitted("   ");
        history.AddSubmitted("/help");
        history.AddSubmitted("  /model list  ");

        Assert.That(history.Entries, Has.Count.EqualTo(2));
        Assert.That(history.Entries[0], Is.EqualTo("/help"));
        Assert.That(history.Entries[1], Is.EqualTo("/model list"));
    }

    [Test]
    public void MovePrevious_Walks_Back_And_Stops_At_Oldest()
    {
        CommandHistoryNavigator history = new();
        history.AddSubmitted("/first");
        history.AddSubmitted("/second");

        bool ok1 = history.TryMovePrevious("", out string entry1);
        bool ok2 = history.TryMovePrevious("", out string entry2);
        bool ok3 = history.TryMovePrevious("", out string entry3);

        Assert.That(ok1, Is.True);
        Assert.That(ok2, Is.True);
        Assert.That(ok3, Is.True);
        Assert.That(entry1, Is.EqualTo("/second"));
        Assert.That(entry2, Is.EqualTo("/first"));
        Assert.That(entry3, Is.EqualTo("/first"));
    }

    [Test]
    public void MoveNext_Returns_Draft_When_Leaving_History()
    {
        CommandHistoryNavigator history = new();
        history.AddSubmitted("/first");
        history.AddSubmitted("/second");

        history.TryMovePrevious("draft", out _);
        history.TryMovePrevious("draft", out _);

        bool ok1 = history.TryMoveNext("ignored", out string entry1);
        bool ok2 = history.TryMoveNext("ignored", out string entry2);

        Assert.That(ok1, Is.True);
        Assert.That(entry1, Is.EqualTo("/second"));
        Assert.That(ok2, Is.True);
        Assert.That(entry2, Is.EqualTo("draft"));
    }

    [Test]
    public void ResetNavigation_Rearms_Draft_Snapshot()
    {
        CommandHistoryNavigator history = new();
        history.AddSubmitted("/one");
        history.AddSubmitted("/two");

        history.TryMovePrevious("draft-a", out _);
        history.ResetNavigation();

        history.TryMovePrevious("draft-b", out string recalled);
        history.TryMoveNext("ignored", out string restored);

        Assert.That(recalled, Is.EqualTo("/two"));
        Assert.That(restored, Is.EqualTo("draft-b"));
    }

    [Test]
    public void Constructor_Enforces_Max_Entries()
    {
        CommandHistoryNavigator history = new(maxEntries: 2);
        history.AddSubmitted("/one");
        history.AddSubmitted("/two");
        history.AddSubmitted("/three");

        Assert.That(history.Entries, Has.Count.EqualTo(2));
        Assert.That(history.Entries[0], Is.EqualTo("/two"));
        Assert.That(history.Entries[1], Is.EqualTo("/three"));
    }

    [TestCase("/help", true)]
    [TestCase(" /help", true)]
    [TestCase("hello", false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    public void IsSlashCommand_Matches_Expected(string value, bool expected)
    {
        Assert.That(CommandHistoryNavigator.IsSlashCommand(value), Is.EqualTo(expected));
    }
}
