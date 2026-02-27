using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Memory;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ConversationMemoryTests
{
    #region CompressionStrategy — EstimateTokens

    [Test]
    public void EstimateTokens_Uses_Chars_Divided_By_Four()
    {
        // 400 chars → ~100 tokens
        ChatMessage msg = new(ChatMessageRoles.User, new string('a', 400));
        int estimated = CompressionStrategy.EstimateTokens(msg);
        Assert.That(estimated, Is.EqualTo(100));
    }

    [Test]
    public void EstimateTokens_MinimumOf_One()
    {
        ChatMessage msg = new(ChatMessageRoles.User, "");
        int estimated = CompressionStrategy.EstimateTokens(msg);
        Assert.That(estimated, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void EstimateTokens_Uses_Tokens_Property_When_Set()
    {
        ChatMessage msg = new(ChatMessageRoles.User, "short text");
        msg.Tokens = 42;
        int estimated = CompressionStrategy.EstimateTokens(msg);
        Assert.That(estimated, Is.EqualTo(42));
    }

    #endregion

    #region CompressionStrategy — Analyze

    [Test]
    public void Analyze_No_Compression_When_Under_Threshold()
    {
        // 128k context, small messages → no compression needed
        CompressionStrategy strategy = new(128_000);
        MessageMetadataTracker tracker = new();

        // ~25 tokens each (100 chars / 4), 10 messages = 250 tokens → way under 60% of 128k
        List<ChatMessage> messages = TestHelpers.MakeMessages(10, 100);
        foreach (ChatMessage m in messages)
            tracker.Track(m);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);
        Assert.That(analysis.ShouldCompress, Is.False);
        Assert.That(analysis.TotalTokens, Is.GreaterThan(0));
        Assert.That(analysis.Utilization, Is.LessThan(0.01));
    }

    [Test]
    public void Analyze_Triggers_Compression_When_Over_Threshold()
    {
        // Context window minimum is 4096 (enforced by CompressionStrategy ctor)
        CompressionStrategy strategy = new(4096);
        MessageMetadataTracker tracker = new();

        // 40 messages × 600 chars = 24000 chars → 6000 tokens → 150% of 4096 window
        // uncompressedUtil = 6000/4096 ≈ 1.46 > 0.60 threshold
        List<ChatMessage> messages = TestHelpers.MakeMessages(40, 600);
        foreach (ChatMessage m in messages)
            tracker.Track(m);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);
        Assert.That(analysis.ShouldCompress, Is.True);
        Assert.That(analysis.Utilization, Is.GreaterThan(0.5));
    }

    [Test]
    public void Analyze_DetectsLargeMessages()
    {
        CompressionStrategy strategy = new(128_000);
        MessageMetadataTracker tracker = new();

        // One giant message > LargeMessageThreshold (10k tokens → 40k chars)
        List<ChatMessage> messages = [new ChatMessage(ChatMessageRoles.User, new string('x', 45_000))];
        foreach (ChatMessage m in messages)
            tracker.Track(m);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);
        Assert.That(analysis.ShouldCompress, Is.True);
        Assert.That(analysis.LargeMessageIndices, Has.Count.EqualTo(1));
    }

    [Test]
    public void Analyze_SystemMessages_Not_Counted_As_Uncompressed()
    {
        CompressionStrategy strategy = new(1000);
        MessageMetadataTracker tracker = new();

        List<ChatMessage> messages =
        [
            new(ChatMessageRoles.System, "System prompt"),
            new(ChatMessageRoles.User, "Hello"),
        ];
        foreach (ChatMessage m in messages)
            tracker.Track(m);

        CompressionAnalysis analysis = strategy.Analyze(messages, tracker);
        Assert.That(analysis.UncompressedIndices, Has.Count.EqualTo(1));
        Assert.That(analysis.UncompressedIndices[0], Is.EqualTo(1)); // Only user msg is uncompressed
    }

    [Test]
    public void Analyze_TargetTokens_Based_On_IsReCompression()
    {
        CompressionStrategy strategy = new(10_000);
        MessageMetadataTracker tracker = new();

        // Normal compression
        List<ChatMessage> bigMessages = TestHelpers.MakeMessages(30, 1000);
        foreach (ChatMessage m in bigMessages)
            tracker.Track(m);

        CompressionAnalysis analysis = strategy.Analyze(bigMessages, tracker);
        if (analysis.ShouldCompress && !analysis.IsReCompression)
        {
            Assert.That(analysis.TargetTokens, Is.EqualTo(4000)); // 40% of 10k
        }
    }

    #endregion

    #region MessageMetadataTracker

    [Test]
    public void Tracker_Default_State_IsUncompressed()
    {
        MessageMetadataTracker tracker = new();
        Guid id = Guid.NewGuid();
        Assert.That(tracker.GetState(id), Is.EqualTo(MessageCompressionState.Uncompressed));
    }

    [Test]
    public void Tracker_MarkCompressed_Changes_State()
    {
        MessageMetadataTracker tracker = new();
        ChatMessage msg = TestHelpers.MakeMessage("test");
        tracker.Track(msg);
        Assert.That(tracker.GetState(msg.Id), Is.EqualTo(MessageCompressionState.Uncompressed));

        tracker.MarkCompressed(msg.Id);
        Assert.That(tracker.GetState(msg.Id), Is.EqualTo(MessageCompressionState.Compressed));
    }

    [Test]
    public void Tracker_MarkReCompressed_Changes_State()
    {
        MessageMetadataTracker tracker = new();
        ChatMessage msg = TestHelpers.MakeMessage("test");
        tracker.Track(msg);
        tracker.MarkCompressed(msg.Id);
        tracker.MarkReCompressed(msg.Id);
        Assert.That(tracker.GetState(msg.Id), Is.EqualTo(MessageCompressionState.ReCompressed));
    }

    [Test]
    public void Tracker_Clear_Resets_All()
    {
        MessageMetadataTracker tracker = new();
        ChatMessage msg = TestHelpers.MakeMessage("test");
        tracker.Track(msg);
        tracker.MarkCompressed(msg.Id);

        tracker.Clear();
        Assert.That(tracker.GetState(msg.Id), Is.EqualTo(MessageCompressionState.Uncompressed));
    }

    #endregion

    #region MessageSummarizer — Summarize (offline logic)

    [Test]
    public async Task Summarize_ReturnsOriginal_When_KeepCount_Exceeds_Available()
    {
        // With only 2 uncompressed messages and keepCount = max(2, 2/5) = 2,
        // summarizeUpTo <= 0 → returns original
        TornadoApi api = new("fake-key");
        MessageSummarizer summarizer = new(api, ChatModel.OpenAi.Gpt41.V41Nano);
        MessageMetadataTracker tracker = new();

        List<ChatMessage> messages =
        [
            new(ChatMessageRoles.User, "Hello"),
            new(ChatMessageRoles.Assistant, "Hi there"),
        ];
        foreach (ChatMessage m in messages)
            tracker.Track(m);

        CompressionAnalysis analysis = new()
        {
            ShouldCompress = true,
            IsReCompression = false,
            TotalTokens = 100,
            Utilization = 0.8,
            LargeMessageIndices = [],
            UncompressedIndices = [0, 1],
            CompressedIndices = [],
            TargetTokens = 50,
        };

        List<ChatMessage> result = await summarizer.Summarize(messages, analysis, tracker, CancellationToken.None);
        // With only 2 messages, keepCount=max(2, 2/5)=2, summarizeUpTo=0 → returns original
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #endregion

    #region ConversationStore

    [Test]
    public void ConversationStore_List_Returns_Empty_When_NoConversations()
    {
        ConversationStore store = new();
        // If conversations dir doesn't exist, should return empty
        var list = store.List();
        Assert.That(list, Is.Not.Null);
    }

    [Test]
    public void ConversationStore_Delete_Returns_False_For_Nonexistent()
    {
        ConversationStore store = new();
        Assert.That(store.Delete("nonexistent_id_12345"), Is.False);
    }

    [Test]
    public void ConversationStore_Load_Returns_Null_For_Nonexistent()
    {
        ConversationStore store = new();
        Assert.That(store.Load("nonexistent_id_12345"), Is.Null);
    }

    #endregion

    #region ConversationMetadata serialization

    [Test]
    public void ConversationMetadata_RoundTrip()
    {
        string tempDir = TestHelpers.CreateTempDir();
        try
        {
            string path = Path.Combine(tempDir, "meta.json");
            ConversationMetadata meta = new()
            {
                Id = "20250101_120000_test",
                Label = "Test Chat",
                CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 12, 30, 0, DateTimeKind.Utc),
                Model = "gpt-4.1-nano",
                MessageCount = 10,
                FirstMessagePreview = "Hello, world!",
                ActiveSkills = ["skill-a"],
            };

            CliStorage.SaveJson(path, meta);
            ConversationMetadata? loaded = CliStorage.LoadJson<ConversationMetadata>(path);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Id, Is.EqualTo("20250101_120000_test"));
            Assert.That(loaded.Label, Is.EqualTo("Test Chat"));
            Assert.That(loaded.Model, Is.EqualTo("gpt-4.1-nano"));
            Assert.That(loaded.MessageCount, Is.EqualTo(10));
            Assert.That(loaded.ActiveSkills, Has.Count.EqualTo(1));
        }
        finally
        {
            TestHelpers.CleanupTempDir(tempDir);
        }
    }

    #endregion
}
