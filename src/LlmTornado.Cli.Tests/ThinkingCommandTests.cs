using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ThinkingCommandTests
{
    [Test]
    public async Task ExecuteAsync_Off_UpdatesRuntimeAndSettings_AndPersists()
    {
        AgentSettings settings = new() { ShowThinking = true };
        bool runtimeShowThinking = true;
        int saveCalls = 0;

        ThinkingCommand command = new(
            settings,
            () => runtimeShowThinking,
            value => runtimeShowThinking = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["off"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeShowThinking, Is.False);
        Assert.That(settings.ShowThinking, Is.False);
        Assert.That(saveCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Toggle_UpdatesRuntimeAndSettings_AndPersists()
    {
        AgentSettings settings = new() { ShowThinking = false };
        bool runtimeShowThinking = false;
        int saveCalls = 0;

        ThinkingCommand command = new(
            settings,
            () => runtimeShowThinking,
            value => runtimeShowThinking = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["toggle"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeShowThinking, Is.True);
        Assert.That(settings.ShowThinking, Is.True);
        Assert.That(saveCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Status_DoesNotPersistOrChange()
    {
        AgentSettings settings = new() { ShowThinking = true };
        bool runtimeShowThinking = true;
        int saveCalls = 0;

        ThinkingCommand command = new(
            settings,
            () => runtimeShowThinking,
            value => runtimeShowThinking = value,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["status"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeShowThinking, Is.True);
        Assert.That(settings.ShowThinking, Is.True);
        Assert.That(saveCalls, Is.EqualTo(0));
    }
}