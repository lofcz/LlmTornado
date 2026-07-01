using LlmTornado.Cli.Commands;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class CommandSystemTests
{
    #region CommandDispatcher

    [Test]
    public void IsCommand_Returns_True_For_Slash_Prefix()
    {
        CommandDispatcher dispatcher = new();
        Assert.That(dispatcher.IsCommand("/help"), Is.True);
        Assert.That(dispatcher.IsCommand("/model list"), Is.True);
        Assert.That(dispatcher.IsCommand("/skill enable foo"), Is.True);
    }

    [Test]
    public void IsCommand_Returns_False_For_Regular_Input()
    {
        CommandDispatcher dispatcher = new();
        Assert.That(dispatcher.IsCommand("hello world"), Is.False);
        Assert.That(dispatcher.IsCommand("what is /help"), Is.False);
        Assert.That(dispatcher.IsCommand(""), Is.False);
    }

    [Test]
    public void IsCommand_Handles_Leading_Whitespace()
    {
        CommandDispatcher dispatcher = new();
        Assert.That(dispatcher.IsCommand("  /help"), Is.True);
    }

    [Test]
    public void Register_Command_Makes_It_Available()
    {
        CommandDispatcher dispatcher = new();
        TestCliCommand cmd = new("test", "Test command", "/test");
        dispatcher.Register(cmd);

        Assert.That(dispatcher.Commands, Does.ContainKey("test"));
    }

    [Test]
    public async Task DispatchAsync_Invokes_Registered_Command()
    {
        CommandDispatcher dispatcher = new();
        TestCliCommand cmd = new("test", "Test command", "/test");
        dispatcher.Register(cmd);

        await dispatcher.DispatchAsync("/test");
        Assert.That(cmd.WasExecuted, Is.True);
        Assert.That(cmd.LastArgs, Is.Empty);
    }

    [Test]
    public async Task DispatchAsync_Passes_Arguments()
    {
        CommandDispatcher dispatcher = new();
        TestCliCommand cmd = new("test", "Test command", "/test <arg>");
        dispatcher.Register(cmd);

        await dispatcher.DispatchAsync("/test arg1 arg2");
        Assert.That(cmd.WasExecuted, Is.True);
        Assert.That(cmd.LastArgs, Has.Length.EqualTo(2));
        Assert.That(cmd.LastArgs![0], Is.EqualTo("arg1"));
        Assert.That(cmd.LastArgs[1], Is.EqualTo("arg2"));
    }

    [Test]
    public async Task DispatchAsync_Passes_Raw_Arguments_To_Raw_Command()
    {
        CommandDispatcher dispatcher = new();
        RawTestCliCommand cmd = new();
        dispatcher.Register(cmd);

        await dispatcher.DispatchAsync("/raw keep going until finished");

        Assert.That(cmd.WasExecuted, Is.True);
        Assert.That(cmd.LastRawArgs, Is.EqualTo("keep going until finished"));
    }

    [Test]
    public async Task DispatchAsync_Unknown_Command_Returns_True()
    {
        CommandDispatcher dispatcher = new();
        // Unknown command should return true (continue loop)
        bool result = await dispatcher.DispatchAsync("/nonexistent");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DispatchAsync_Empty_Slash_Returns_True()
    {
        CommandDispatcher dispatcher = new();
        bool result = await dispatcher.DispatchAsync("/");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Register_Overwrites_Command_With_Same_Name()
    {
        CommandDispatcher dispatcher = new();

        TestCliCommand cmd1 = new("test", "First", "/test");
        TestCliCommand cmd2 = new("test", "Second", "/test");

        dispatcher.Register(cmd1);
        dispatcher.Register(cmd2);

        Assert.That(dispatcher.Commands, Has.Count.EqualTo(1));
        Assert.That(dispatcher.Commands["test"].Description, Is.EqualTo("Second"));
    }

    [Test]
    public async Task DispatchAsync_Is_CaseInsensitive()
    {
        CommandDispatcher dispatcher = new();
        TestCliCommand cmd = new("Help", "Help", "/help");
        dispatcher.Register(cmd);

        await dispatcher.DispatchAsync("/help");
        Assert.That(cmd.WasExecuted, Is.True);
    }

    #endregion

    #region ClearCommand

    [Test]
    public async Task ClearCommand_Returns_True()
    {
        ClearCommand cmd = new();
        Assert.That(cmd.Name, Is.EqualTo("clear"));

        try
        {
            bool result = await cmd.ExecuteAsync([]);
            Assert.That(result, Is.True);
        }
        catch (IOException)
        {
            // Console.Clear() throws in non-interactive test environments
            Assert.Pass("Console.Clear() not available in test runner.");
        }
    }

    #endregion

    #region HelpCommand

    [Test]
    public async Task HelpCommand_Returns_True()
    {
        CommandDispatcher dispatcher = new();
        HelpCommand cmd = new(dispatcher);
        dispatcher.Register(cmd);

        bool result = await cmd.ExecuteAsync([]);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HelpCommand_WithArg_Returns_True()
    {
        CommandDispatcher dispatcher = new();
        HelpCommand cmd = new(dispatcher);
        dispatcher.Register(cmd);

        bool result = await cmd.ExecuteAsync(["help"]);
        Assert.That(result, Is.True);
    }

    #endregion

    /// <summary>
    /// Test implementation of ICliCommand for dispatching tests.
    /// </summary>
    private class TestCliCommand : ICliCommand
    {
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public bool WasExecuted { get; private set; }
        public string[]? LastArgs { get; private set; }

        public TestCliCommand(string name, string description, string usage)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }

        public Task<bool> ExecuteAsync(string[] args)
        {
            WasExecuted = true;
            LastArgs = args;
            return Task.FromResult(true);
        }
    }

    private sealed class RawTestCliCommand : IRawCliCommand
    {
        public string Name => "raw";
        public string Description => "Raw test command";
        public string Usage => "/raw <text>";
        public bool WasExecuted { get; private set; }
        public string? LastRawArgs { get; private set; }

        public Task<bool> ExecuteAsync(string[] args) =>
            ExecuteRawAsync(string.Join(' ', args));

        public Task<bool> ExecuteRawAsync(string rawArgs)
        {
            WasExecuted = true;
            LastRawArgs = rawArgs;
            return Task.FromResult(true);
        }
    }
}
