using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Regression tests for the unified conversation-context lifecycle: the runtime conversation is the single
/// canonical store, ConversationMemoryManager coordinates compression/persistence, and the managed runtime
/// config binds them together. These tests are fully offline (no model calls).
/// </summary>
[TestFixture]
public class ManagedConversationLifecycleTests
{
    private static readonly ChatModel Model = ChatModel.OpenAi.Gpt41.V41Nano;

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

    private ConversationMemoryManager NewManager(int contextWindow = 128_000) =>
        new(new TornadoApi("fake-key"), Model, contextWindow, _store);

    // ── Gap A: persistence is live from the first turn ──────────────────────────────

    [Test]
    public void EnsureActiveConversation_BindsId_And_SyncFrom_Persists_FullSet()
    {
        ConversationMemoryManager mgr = NewManager();
        mgr.EnsureActiveConversation();

        Assert.That(mgr.ConversationId, Is.Not.Null, "Startup should bind a conversation id so persistence is active.");

        // A realistic turn captured from the runtime: user + tool-call + tool-result + final assistant.
        List<ChatMessage> fullTurn =
        [
            new(ChatMessageRoles.User, "do the thing"),
            new(ChatMessageRoles.Assistant, "calling a tool"),
            new(ChatMessageRoles.Tool, "tool result payload"),
            new(ChatMessageRoles.Assistant, "done"),
        ];
        mgr.SyncFrom(fullTurn);

        List<ChatMessage>? loaded = _store.Load(mgr.ConversationId!);
        Assert.That(loaded, Is.Not.Null);
        // The full set is persisted (4 messages) — including the intermediate tool-call/tool-result turns
        // that the old per-message path dropped (it recorded only the user message and the final assistant
        // reply, i.e. 2). Capturing the complete set is what lets compression act on real context.
        Assert.That(loaded!, Has.Count.EqualTo(4));
    }

    // ── Gap B: load binds the id so subsequent turns keep writing to the same conversation ──

    [Test]
    public void LoadConversation_ById_Binds_And_Continues_Persisting_SameId()
    {
        string savedId = _store.Save(
            [new(ChatMessageRoles.User, "hello"), new(ChatMessageRoles.Assistant, "hi")],
            Model.Name, null, "seed");

        ConversationMemoryManager mgr = NewManager();
        mgr.LoadConversation(savedId);

        Assert.That(mgr.ConversationId, Is.EqualTo(savedId), "Loading must bind the id for incremental persistence.");
        Assert.That(mgr.Messages, Has.Count.EqualTo(2));

        // A new turn should upsert into the SAME conversation, not create a new one.
        int convosBefore = _store.List().Count;
        mgr.SyncFrom([.. mgr.Messages, new ChatMessage(ChatMessageRoles.User, "another turn")]);

        Assert.That(_store.List().Count, Is.EqualTo(convosBefore), "Should update the same conversation, not fork a new one.");
        List<ChatMessage>? reloaded = _store.Load(savedId);
        Assert.That(reloaded!, Has.Count.EqualTo(3));
    }

    // ── Hard token budget guard (deterministic, no model call) ──────────────────────

    [Test]
    public async Task MaybeSummarize_EnforcesHardBudget_Drops_Oldest_And_Notifies()
    {
        // Minimum window is 4096; budget = 90% = ~3686 tokens. Two ~3000-token messages (12k chars) exceed it.
        // With exactly two uncompressed messages the summarizer is a no-op (keepCount=2 ⇒ summarizeUpTo=0),
        // so no model call happens and only the hard-budget guard runs.
        ConversationMemoryManager mgr = NewManager(contextWindow: 4096);

        int trimmed = 0;
        mgr.ContextTrimmed += n => trimmed = n;

        mgr.SyncFrom(
        [
            new(ChatMessageRoles.User, new string('a', 12_000)),
            new(ChatMessageRoles.User, new string('b', 12_000)),
        ]);

        bool changed = await mgr.MaybeSummarize();

        Assert.That(changed, Is.True, "Budget enforcement counts as a change so the runtime gets re-synced.");
        Assert.That(trimmed, Is.GreaterThan(0), "Trimming must be surfaced, never silent.");

        int budget = (int)(4096 * 0.90);
        int total = mgr.GetMessagesForAgent().Sum(CompressionStrategy.EstimateTokens);
        Assert.That(total, Is.LessThanOrEqualTo(budget), "Payload must be within the hard budget after enforcement.");
        // The most recent message is always kept.
        Assert.That(mgr.GetMessagesForAgent(), Is.Not.Empty);
    }

    [Test]
    public async Task MaybeSummarize_NoOp_When_Under_Budget()
    {
        ConversationMemoryManager mgr = NewManager(); // 128k window
        mgr.SyncFrom([new(ChatMessageRoles.User, "short"), new(ChatMessageRoles.Assistant, "also short")]);

        bool changed = await mgr.MaybeSummarize();

        Assert.That(changed, Is.False);
        Assert.That(mgr.GetMessagesForAgent(), Has.Count.EqualTo(2));
    }

    // ── Managed config feeds loaded history to the model (CLI analogue of the Blazor data-loss bug) ──

    [Test]
    public void ManagedConfig_LoadConversation_Rehydrates_Runtime_Conversation()
    {
        string savedId = _store.Save(
        [
            new(ChatMessageRoles.User, "my name is Sam"),
            new(ChatMessageRoles.Assistant, "noted"),
            new(ChatMessageRoles.User, "what is my name?"),
            new(ChatMessageRoles.Assistant, "Sam"),
        ], Model.Name, null, "history");

        ConversationMemoryManager mgr = NewManager();
        TornadoAgent agent = new(new TornadoApi("fake-key"), Model, "t", "instructions");
        ManagedConversationRuntimeConfiguration config = new(agent, mgr);

        config.LoadConversation(savedId);

        // The runtime conversation (what is actually sent to the model) must contain the loaded history,
        // so the first post-load turn has full context — and saving it back cannot truncate it.
        List<ChatMessage> runtimeMessages = config.GetMessages();
        Assert.That(runtimeMessages, Has.Count.EqualTo(4));
        Assert.That(mgr.ConversationId, Is.EqualTo(savedId));
    }

    [Test]
    public void ManagedConfig_Ctor_Rehydrates_From_Memory_For_Rebuilds()
    {
        // Simulate a rebuild (model/agent switch): memory already holds history; a freshly constructed
        // config must restore it into the runtime conversation without any manual copy.
        ConversationMemoryManager mgr = NewManager();
        mgr.EnsureActiveConversation();
        mgr.SyncFrom([new(ChatMessageRoles.User, "remember this"), new(ChatMessageRoles.Assistant, "ok")]);

        TornadoAgent agent = new(new TornadoApi("fake-key"), Model, "t", "instructions");
        ManagedConversationRuntimeConfiguration config = new(agent, mgr);

        Assert.That(config.GetMessages(), Has.Count.EqualTo(2), "History must survive a runtime rebuild.");
    }
}
