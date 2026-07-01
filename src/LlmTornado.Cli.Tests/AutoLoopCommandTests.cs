using LlmTornado.Cli.Commands;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class AutoLoopCommandTests
{
    [Test]
    public async Task ExecuteRawAsync_RepeatsStatementUntilControllerStops()
    {
        AutoLoopController controller = new();
        List<string> statements = [];

        AutoLoopCommand command = new(
            controller,
            (statement, _) =>
            {
                statements.Add(statement);
                if (statements.Count == 3)
                    controller.Stop();

                return Task.CompletedTask;
            });

        bool result = await command.ExecuteRawAsync("keep working");

        Assert.That(result, Is.True);
        Assert.That(statements, Is.EqualTo(new[] { "keep working", "keep working", "keep working" }));
        Assert.That(controller.IsRunning, Is.False);
    }

    [Test]
    public async Task ExecuteRawAsync_EmptyStatement_DoesNotStartLoop()
    {
        AutoLoopController controller = new();
        int calls = 0;

        AutoLoopCommand command = new(
            controller,
            (_, _) =>
            {
                calls++;
                return Task.CompletedTask;
            });

        bool result = await command.ExecuteRawAsync("   ");

        Assert.That(result, Is.True);
        Assert.That(calls, Is.EqualTo(0));
        Assert.That(controller.IsRunning, Is.False);
    }
}
