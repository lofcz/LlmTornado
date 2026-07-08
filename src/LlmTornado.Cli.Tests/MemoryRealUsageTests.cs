using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Phase-2 real-token accounting: provider-reported usage replaces the chars/4 guess as the
/// context baseline, is sealed at the SyncFrom point, and is dropped whenever history rewrites.
/// No test here touches the network (summarization no-ops with ≤2 uncompressed messages).
/// </summary>
[TestFixture]
public class MemoryRealUsageTests
{
    private string _tempDir = null!;
    private SqliteConversationStore _store = null!;
    private TornadoApi _api = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tornado-memusage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _store = new SqliteConversationStore(Path.Combine(_tempDir, "test.db"), Path.Combine(_tempDir, "attachments"));
        _api = new TornadoApi(new Uri("http://127.0.0.1:9"), "unused");
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* locked on CI is fine */ }
    }

    private ConversationMemoryManager CreateManager(int? cap = null) =>
        new(_api, new ChatModel("m", LLmProviders.Custom), 128_000, _store, compressionContextTokenCap: cap);

    private static ChatMessage Msg(int chars, ChatMessageRoles role = ChatMessageRoles.User) =>
        new(role, new string('a', chars));

    [Test]
    public void NoUsageReported_FallsBackToEstimate()
    {
        ConversationMemoryManager memory = CreateManager();
        memory.SyncFrom([Msg(400), Msg(400)]);

        Assert.That(memory.HasActualTokenCount, Is.False);
        Assert.That(memory.EstimateCurrentTokens(), Is.EqualTo(200));
    }

    [Test]
    public void ReportedUsage_IsSealedAtSync_AndReplacesEstimate()
    {
        ConversationMemoryManager memory = CreateManager();

        memory.ReportActualUsage(promptTokens: 5000, completionTokens: 300);
        Assert.That(memory.HasActualTokenCount, Is.False, "usage must not count before sync");

        memory.SyncFrom([Msg(400), Msg(400)]);

        Assert.That(memory.HasActualTokenCount, Is.True);
        Assert.That(memory.EstimateCurrentTokens(), Is.EqualTo(5300));
    }

    [Test]
    public void MessagesAppendedAfterSeal_AddEstimatedTail()
    {
        ConversationMemoryManager memory = CreateManager();
        memory.ReportActualUsage(5000, 300);
        memory.SyncFrom([Msg(400)]);

        memory.AddMessage(Msg(800)); // ~200 tokens

        Assert.That(memory.EstimateCurrentTokens(), Is.EqualTo(5300 + 200));
    }

    [Test]
    public async Task NoOpTurn_KeepsSealedUsage()
    {
        ConversationMemoryManager memory = CreateManager(); // huge window → nothing triggers
        memory.ReportActualUsage(5000, 300);
        memory.SyncFrom([Msg(400), Msg(400)]);

        bool changed = await memory.MaybeSummarize();

        Assert.That(changed, Is.False);
        Assert.That(memory.HasActualTokenCount, Is.True);
    }

    [Test]
    public async Task HardBudgetTrim_InvalidatesSealedUsage()
    {
        // Cap the window at 4096 → hard budget ≈ 3686. Two ~3000-token messages exceed it, and
        // with only 2 uncompressed messages summarization no-ops, so the trim path runs alone.
        ConversationMemoryManager memory = CreateManager(cap: 4096);
        int trimmed = 0;
        memory.ContextTrimmed += dropped => trimmed = dropped;

        memory.ReportActualUsage(6000, 100);
        memory.SyncFrom([Msg(12_000), Msg(12_000)]);

        bool changed = await memory.MaybeSummarize();

        Assert.That(changed, Is.True);
        Assert.That(trimmed, Is.EqualTo(1), "oldest message should be dropped");
        Assert.That(memory.HasActualTokenCount, Is.False, "rewritten history must drop the sealed figure");
    }

    [Test]
    public void NewConversation_InvalidatesSealedUsage()
    {
        ConversationMemoryManager memory = CreateManager();
        memory.ReportActualUsage(5000, 300);
        memory.SyncFrom([Msg(400)]);

        memory.NewConversation();

        Assert.That(memory.HasActualTokenCount, Is.False);
        Assert.That(memory.EstimateCurrentTokens(), Is.Zero);
    }

    [Test]
    public void LoadConversation_InvalidatesSealedUsage()
    {
        ConversationMemoryManager memory = CreateManager();
        memory.ReportActualUsage(5000, 300);
        memory.SyncFrom([Msg(400)]);

        memory.LoadConversation([Msg(400), Msg(400)]);

        Assert.That(memory.HasActualTokenCount, Is.False);
        Assert.That(memory.EstimateCurrentTokens(), Is.EqualTo(200));
    }
}

[TestFixture]
public class CompressionStrategyRealTokenTests
{
    private static (CompressionStrategy Strategy, MessageMetadataTracker Tracker) Create(int window = 4096)
        => (new CompressionStrategy(window), new MessageMetadataTracker());

    private static List<ChatMessage> Track(MessageMetadataTracker tracker, List<ChatMessage> messages)
    {
        foreach (ChatMessage m in messages)
            tracker.Track(m);
        return messages;
    }

    [Test]
    public void ActualTotal_TriggersCompression_WhenEstimateWouldNot()
    {
        (CompressionStrategy strategy, MessageMetadataTracker tracker) = Create(4096);
        List<ChatMessage> messages = Track(tracker, TestHelpers.MakeMessages(10, 100)); // ~250 tokens estimated

        CompressionAnalysis withoutReal = strategy.Analyze(messages, tracker);
        CompressionAnalysis withReal = strategy.Analyze(messages, tracker, actualTotalTokens: 4000); // 0.98 util

        Assert.That(withoutReal.ShouldCompress, Is.False);
        Assert.That(withReal.ShouldCompress, Is.True);
        Assert.That(withReal.TotalTokens, Is.EqualTo(4000));
    }

    [Test]
    public void ActualTotal_SmallerThanEstimate_IsIgnored()
    {
        (CompressionStrategy strategy, MessageMetadataTracker tracker) = Create(4096);
        List<ChatMessage> messages = Track(tracker, TestHelpers.MakeMessages(40, 600)); // ~6000 tokens estimated

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker, actualTotalTokens: 100);

        Assert.That(analysis.TotalTokens, Is.EqualTo(6000));
        Assert.That(analysis.ShouldCompress, Is.True);
    }

    [Test]
    public void LargeMessage_AloneInSmallContext_DoesNotTrigger()
    {
        // One 11k-token message in a 128k window: utilization ~0.09 < 0.50 floor → with
        // tool-result truncation in place this is not worth a full history rewrite.
        (CompressionStrategy strategy, MessageMetadataTracker tracker) = Create(128_000);
        List<ChatMessage> messages = Track(tracker, [new ChatMessage(ChatMessageRoles.User, new string('a', 44_000))]);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);

        Assert.That(analysis.ShouldCompress, Is.False);
    }

    [Test]
    public void LargeMessage_AtHighUtilization_StillTriggers()
    {
        // One 11k-token message in a 16k window: utilization ~0.69 ≥ 0.50 floor → triggers even
        // though 0.69 is below the 0.80 general threshold.
        (CompressionStrategy strategy, MessageMetadataTracker tracker) = Create(16_000);
        List<ChatMessage> messages = Track(tracker, [new ChatMessage(ChatMessageRoles.User, new string('a', 44_000))]);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);

        Assert.That(analysis.ShouldCompress, Is.True);
    }
}
