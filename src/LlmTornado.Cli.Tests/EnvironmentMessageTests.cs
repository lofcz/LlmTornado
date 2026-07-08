using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Phase-2 stable-prompt-prefix behavior: volatile session facts (cwd, date) live in a pinned
/// &lt;env&gt; user message maintained by the managed runtime config, not in the system prompt.
/// </summary>
[TestFixture]
public class EnvironmentMessageTests
{
    private string _tempDir = null!;
    private SqliteConversationStore _store = null!;
    private TornadoApi _api = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tornado-envmsg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new SqliteConversationStore(Path.Combine(_tempDir, "test.db"), Path.Combine(_tempDir, "attachments"));
        _api = new TornadoApi(new Uri("http://127.0.0.1:9"), "unused");
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private ManagedConversationRuntimeConfiguration CreateConfig(ConversationMemoryManager memory, string cwd = @"C:\work\project")
    {
        TornadoAgent agent = new(_api, new ChatModel("m", LLmProviders.Custom), "Agent", "instructions", streaming: false);
        return new ManagedConversationRuntimeConfiguration(
            agent,
            memory,
            () => new ChatMessage(ChatMessageRoles.User,
                $"{AgentBuilder.EnvironmentTag}\nworking_directory: {cwd}\n</env>"));
    }

    private ConversationMemoryManager CreateMemory() =>
        new(_api, new ChatModel("m", LLmProviders.Custom), 128_000, _store);

    [Test]
    public void Rehydrate_PinsEnvMessageFirst()
    {
        ConversationMemoryManager memory = CreateMemory();
        memory.SyncFrom([new ChatMessage(ChatMessageRoles.User, "hello")]);

        ManagedConversationRuntimeConfiguration config = CreateConfig(memory);

        List<ChatMessage> messages = config.GetMessages();
        Assert.That(messages, Has.Count.EqualTo(2));
        Assert.That(messages[0].Content, Does.StartWith(AgentBuilder.EnvironmentTag));
        Assert.That(messages[1].Content, Is.EqualTo("hello"));
    }

    [Test]
    public void Rehydrate_ReplacesStaleEnvMessage_NeverDuplicates()
    {
        ConversationMemoryManager memory = CreateMemory();
        // Simulate persisted history that already contains an old env message (previous session).
        memory.SyncFrom([
            new ChatMessage(ChatMessageRoles.User, $"{AgentBuilder.EnvironmentTag}\nworking_directory: C:\\old\\dir\n</env>"),
            new ChatMessage(ChatMessageRoles.User, "hello"),
            new ChatMessage(ChatMessageRoles.Assistant, "hi!"),
        ]);

        ManagedConversationRuntimeConfiguration config = CreateConfig(memory, cwd: @"C:\new\dir");

        List<ChatMessage> messages = config.GetMessages();
        List<ChatMessage> envMessages = messages
            .Where(m => m.Content?.StartsWith(AgentBuilder.EnvironmentTag, StringComparison.Ordinal) == true)
            .ToList();

        Assert.That(envMessages, Has.Count.EqualTo(1), "stale env message must be replaced, not accumulated");
        Assert.That(envMessages[0].Content, Does.Contain(@"C:\new\dir"));
        Assert.That(messages[0], Is.SameAs(envMessages[0]), "env message must be pinned first");
        Assert.That(messages.Select(m => m.Content), Does.Contain("hello").And.Contain("hi!"));
    }

}
