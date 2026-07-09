using LlmTornado.Chat;
using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ConversationRecencyTests
{
    private string _tempDir = null!;
    private SqliteConversationStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _store = new SqliteConversationStore(
            Path.Combine(_tempDir, "test.db"),
            Path.Combine(_tempDir, "attachments"));
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        TestHelpers.CleanupTempDir(_tempDir);
    }

    [Test]
    public void GetMostRecentConversationId_EmptyStore_ReturnsNull()
    {
        Assert.That(_store.GetMostRecentConversationId(), Is.Null);
    }

    [Test]
    public void GetMostRecentConversationId_ReturnsLatestUpdated()
    {
        string first = _store.Save([new ChatMessage(ChatMessageRoles.User, "one")], "m", null, "first");
        string second = _store.Save([new ChatMessage(ChatMessageRoles.User, "two")], "m", null, "second");
        Assert.That(second, Is.Not.EqualTo(first));

        // Touch the first conversation so it becomes the most recently updated.
        Thread.Sleep(20); // ensure a strictly later updated_at timestamp
        _store.Save(
            [new ChatMessage(ChatMessageRoles.User, "one"), new ChatMessage(ChatMessageRoles.Assistant, "reply")],
            "m", null, "first", existingId: first);

        Assert.That(_store.GetMostRecentConversationId(), Is.EqualTo(first));
    }
}

[TestFixture]
public class ConfigCommandTests
{
    private AgentSettings _settings = null!;
    private int _samplingApplied;
    private int _rebuilt;
    private int _persisted;
    private ConfigCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new AgentSettings();
        _samplingApplied = 0;
        _rebuilt = 0;
        _persisted = 0;
        _command = new ConfigCommand(
            _settings,
            applySamplingOptions: () => _samplingApplied++,
            rebuildAgent: () => _rebuilt++,
            modelInfo: () => ("test-model", 32768),
            persistSettings: _ => _persisted++);
    }

    [Test]
    public async Task Temperature_Set_AppliesAndPersists()
    {
        await _command.ExecuteAsync(["temperature", "0.7"]);

        Assert.That(_settings.Temperature, Is.EqualTo(0.7).Within(1e-9));
        Assert.That(_samplingApplied, Is.EqualTo(1));
        Assert.That(_persisted, Is.EqualTo(1));
        Assert.That(_rebuilt, Is.Zero);
    }

    [Test]
    public async Task Temperature_Off_ClearsSetting()
    {
        _settings.Temperature = 1.2;
        await _command.ExecuteAsync(["temperature", "off"]);
        Assert.That(_settings.Temperature, Is.Null);
        Assert.That(_samplingApplied, Is.EqualTo(1));
    }

    [TestCase("2.5")]
    [TestCase("-1")]
    [TestCase("hot")]
    public async Task Temperature_Invalid_Rejected(string value)
    {
        await _command.ExecuteAsync(["temperature", value]);
        Assert.That(_settings.Temperature, Is.Null);
        Assert.That(_samplingApplied, Is.Zero);
        Assert.That(_persisted, Is.Zero);
    }

    [Test]
    public async Task MaxOutputTokens_Set_AppliesAndPersists()
    {
        await _command.ExecuteAsync(["max-output-tokens", "2048"]);
        Assert.That(_settings.MaxOutputTokens, Is.EqualTo(2048));
        Assert.That(_samplingApplied, Is.EqualTo(1));
        Assert.That(_persisted, Is.EqualTo(1));
    }

    [TestCase("0")]
    [TestCase("-5")]
    [TestCase("lots")]
    public async Task MaxOutputTokens_Invalid_Rejected(string value)
    {
        await _command.ExecuteAsync(["max-output-tokens", value]);
        Assert.That(_settings.MaxOutputTokens, Is.Null);
        Assert.That(_persisted, Is.Zero);
    }

    [Test]
    public async Task SystemPrompt_MissingFile_Rejected()
    {
        await _command.ExecuteAsync(["system-prompt", Path.Combine(Path.GetTempPath(), "does-not-exist-9a7b.md")]);
        Assert.That(_settings.SystemPromptFile, Is.Null);
        Assert.That(_rebuilt, Is.Zero);
    }

    [Test]
    public async Task SystemPrompt_ExistingFile_SetsAndRebuilds()
    {
        string dir = TestHelpers.CreateTempDir();
        try
        {
            string promptFile = Path.Combine(dir, "prompt.md");
            File.WriteAllText(promptFile, "You are a terse assistant.");

            await _command.ExecuteAsync(["system-prompt", promptFile]);

            Assert.That(_settings.SystemPromptFile, Is.EqualTo(Path.GetFullPath(promptFile)));
            Assert.That(_rebuilt, Is.EqualTo(1));
            Assert.That(_persisted, Is.EqualTo(1));
        }
        finally
        {
            TestHelpers.CleanupTempDir(dir);
        }
    }

    [Test]
    public async Task SystemPrompt_Off_ClearsAndRebuilds()
    {
        _settings.SystemPromptFile = "somewhere.md";
        await _command.ExecuteAsync(["system-prompt", "off"]);
        Assert.That(_settings.SystemPromptFile, Is.Null);
        Assert.That(_rebuilt, Is.EqualTo(1));
    }
}
