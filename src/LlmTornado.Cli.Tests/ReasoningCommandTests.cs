using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ReasoningCommandTests
{
    private AgentSettings _settings = null!;
    private List<string?> _applied = null!;
    private List<AgentSettings> _persisted = null!;
    private ReasoningCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AgentSettings();
        _applied = [];
        _persisted = [];
        _command = new ReasoningCommand(
            _settings,
            applyEffort: effort => { _settings.ReasoningEffort = effort; _applied.Add(effort); },
            persistSettings: s => _persisted.Add(s));
    }

    [TestCase("high", "high")]
    [TestCase("XHIGH", "xhigh")]
    [TestCase("minimal", "minimal")]
    public async Task SetLevel_AppliesAndPersists(string arg, string expected)
    {
        await _command.ExecuteAsync([arg]);

        Assert.That(_applied, Is.EqualTo(new[] { expected }));
        Assert.That(_settings.ReasoningEffort, Is.EqualTo(expected));
        Assert.That(_persisted, Has.Count.EqualTo(1));
    }

    [TestCase("default")]
    [TestCase("DEFAULT")]
    public async Task Default_ClearsPersistedEffort(string arg)
    {
        _settings.ReasoningEffort = "high";
        await _command.ExecuteAsync([arg]);

        Assert.That(_applied, Is.EqualTo(new string?[] { null }));
        Assert.That(_settings.ReasoningEffort, Is.Null);
    }

    [Test]
    public async Task Off_MapsToNone()
    {
        await _command.ExecuteAsync(["off"]);
        Assert.That(_applied, Is.EqualTo(new[] { "none" }));
    }

    [Test]
    public async Task InvalidLevel_DoesNotApplyOrPersist()
    {
        await _command.ExecuteAsync(["turbo"]);
        Assert.That(_applied, Is.Empty);
        Assert.That(_persisted, Is.Empty);
    }

    [Test]
    public async Task NoArgs_OnlyShowsStatus()
    {
        await _command.ExecuteAsync([]);
        Assert.That(_applied, Is.Empty);
        Assert.That(_persisted, Is.Empty);
    }
}
