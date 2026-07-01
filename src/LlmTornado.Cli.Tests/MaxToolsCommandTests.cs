using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class MaxToolsCommandTests
{
    [Test]
    public async Task ExecuteAsync_Number_UpdatesRuntimeAndSettings_AndPersists()
    {
        AgentSettings settings = new() { MaxTools = 25 };
        int runtimeMaxTools = 25;
        int saveCalls = 0;

        MaxToolsCommand command = new(
            settings,
            value => runtimeMaxTools = value,
            () => 40,
            () => true,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["12"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeMaxTools, Is.EqualTo(12));
        Assert.That(settings.MaxTools, Is.EqualTo(12));
        Assert.That(saveCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAsync_Status_DoesNotPersistOrChange()
    {
        AgentSettings settings = new() { MaxTools = 25 };
        int runtimeMaxTools = 25;
        int saveCalls = 0;

        MaxToolsCommand command = new(
            settings,
            value => runtimeMaxTools = value,
            () => 10,
            () => false,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["status"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeMaxTools, Is.EqualTo(25));
        Assert.That(settings.MaxTools, Is.EqualTo(25));
        Assert.That(saveCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_InvalidValue_DoesNotPersistOrChange()
    {
        AgentSettings settings = new() { MaxTools = 25 };
        int runtimeMaxTools = 25;
        int saveCalls = 0;

        MaxToolsCommand command = new(
            settings,
            value => runtimeMaxTools = value,
            () => 10,
            () => false,
            _ => saveCalls++);

        bool result = await command.ExecuteAsync(["0"]);

        Assert.That(result, Is.True);
        Assert.That(runtimeMaxTools, Is.EqualTo(25));
        Assert.That(settings.MaxTools, Is.EqualTo(25));
        Assert.That(saveCalls, Is.EqualTo(0));
    }
}
