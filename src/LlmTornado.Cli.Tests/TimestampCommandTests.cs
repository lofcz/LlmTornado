using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class TimestampCommandTests
{
    [Test]
    public async Task ExecuteAsync_Off_UpdatesRuntimeAndSettings_AndPersists()
    {
        AgentSettings settings = new() { ShowTimestamps = true };
        bool enabled = true;
        int saveCalls = 0;

        TimestampCommand command = new(
            settings,
            () => enabled,
            value => enabled = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["off"]);

        Assert.That(result, Is.True);
        Assert.That(enabled, Is.False);
        Assert.That(settings.ShowTimestamps, Is.False);
        Assert.That(saveCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Toggle_UpdatesRuntimeAndSettings_AndPersists()
    {
        AgentSettings settings = new() { ShowTimestamps = false };
        bool enabled = false;
        int saveCalls = 0;

        TimestampCommand command = new(
            settings,
            () => enabled,
            value => enabled = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["toggle"]);

        Assert.That(result, Is.True);
        Assert.That(enabled, Is.True);
        Assert.That(settings.ShowTimestamps, Is.True);
        Assert.That(saveCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Status_DoesNotPersistOrChange()
    {
        AgentSettings settings = new() { ShowTimestamps = true };
        bool enabled = true;
        int saveCalls = 0;

        TimestampCommand command = new(
            settings,
            () => enabled,
            value => enabled = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["status"]);

        Assert.That(result, Is.True);
        Assert.That(enabled, Is.True);
        Assert.That(settings.ShowTimestamps, Is.True);
        Assert.That(saveCalls, Is.EqualTo(0));
    }
}
