using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Tests.ContextController;

[TestFixture]
[Category("Unit")] 
public class ContextWindowSummarizerOrderTests
{
    private static ChatModel TestModel => ChatModel.OpenAi.Gpt35.Turbo;

    private static ContextWindowMessageSummarizer CreateSummarizer(MessageMetadataStore store)
    {
        // Use dummy API key; summarizer catches API call failures and returns placeholders
        var api = new TornadoApi(LLmProviders.OpenAi, "dummy-key");
        var options = new ContextWindowCompressionOptions
        {
            // Force initial compression regardless of total utilization
            UncompressedCompressionThreshold = 0.0,
            TargetUtilization = 0.0,
            // Avoid re-compression path
            CompressedReCompressionThreshold = 1.1,
            ReCompressionTarget = 0.2,
            SummaryModel = TestModel,
            LargeMessageThreshold = 10000
        };
        return new ContextWindowMessageSummarizer(api, TestModel, store, options);
    }

    private static (List<ChatMessage> messages, MessageMetadataStore store) BuildConversation(params (ChatMessageRoles role, string content)[] parts)
    {
        var msgs = new List<ChatMessage>();
        var store = new MessageMetadataStore();
        foreach (var p in parts)
        {
            var m = new ChatMessage(p.role, p.content);
            msgs.Add(m);
            store.Track(m); // All messages start as Uncompressed unless System (still tracked)
        }
        return (msgs, store);
    }

    [Test]
    public async Task PreservesOrder_AcrossSystemAndSummaries_SingleGroup()
    {
        // Arrange: S0, U1, U2, U3, S1, U4
        var (messages, store) = BuildConversation(
            (ChatMessageRoles.System, "S0"),
            (ChatMessageRoles.User, new string('A', 400)),
            (ChatMessageRoles.Assistant, new string('B', 400)),
            (ChatMessageRoles.User, new string('C', 400)),
            (ChatMessageRoles.System, "S1"),
            (ChatMessageRoles.User, new string('D', 400))
        );

        var summarizer = CreateSummarizer(store);

        // Chunk size large enough to merge U1..U3 into a single group
        var options = new MessageCompressionOptions
        {
            ChunkSize = 5000,
            PreserveSystemmessages = true,
            CompressToolCallmessages = true,
            SummaryModel = TestModel,
            SummaryPrompt = "test",
            MaxSummaryTokens = 64
        };

        // Act
        var rebuilt = await summarizer.SummarizeMessages(messages, options);

        // Assert order: [S0] [Summary(U1..U3)] [S1] [Summary(U4)]
        Assert.That(rebuilt, Is.Not.Null);
        Assert.That(rebuilt.Count, Is.EqualTo(4));
        Assert.That(rebuilt[0].Role, Is.EqualTo(ChatMessageRoles.System));
        Assert.That(rebuilt[1].Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(rebuilt[2].Role, Is.EqualTo(ChatMessageRoles.System));
        Assert.That(rebuilt[3].Role, Is.EqualTo(ChatMessageRoles.Assistant));

        // Verify summary prefixes to ensure they are summaries (not original assistant messages)
        Assert.That(rebuilt[1].GetMessageContent(), Does.StartWith("[Compressed summary]:"));
        Assert.That(rebuilt[3].GetMessageContent(), Does.StartWith("[Compressed summary]:"));
    }

    [Test]
    public async Task PreservesOrder_WithChunkSplitting_MultipleSummaries()
    {
        // Arrange: S0, U1, U2, U3, S1, U4
        var (messages, store) = BuildConversation(
            (ChatMessageRoles.System, "S0"),
            (ChatMessageRoles.User, new string('A', 200)),
            (ChatMessageRoles.Assistant, new string('B', 200)),
            (ChatMessageRoles.User, new string('C', 200)),
            (ChatMessageRoles.System, "S1"),
            (ChatMessageRoles.User, new string('D', 200))
        );

        var summarizer = CreateSummarizer(store);

        // Small chunk size to force splits between U1/U2/U3
        var options = new MessageCompressionOptions
        {
            ChunkSize = 250, // each ~200, so each becomes its own summary
            PreserveSystemmessages = true,
            CompressToolCallmessages = true,
            SummaryModel = TestModel,
            SummaryPrompt = "test",
            MaxSummaryTokens = 64
        };

        // Act
        var rebuilt = await summarizer.SummarizeMessages(messages, options);

        // Assert order: [S0] [Sum(U1)] [Sum(U2)] [Sum(U3)] [S1] [Sum(U4)]
        Assert.That(rebuilt, Is.Not.Null);
        Assert.That(rebuilt.Count, Is.EqualTo(6));
        Assert.That(rebuilt[0].Role, Is.EqualTo(ChatMessageRoles.System));
        Assert.That(rebuilt[1].Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(rebuilt[2].Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(rebuilt[3].Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(rebuilt[4].Role, Is.EqualTo(ChatMessageRoles.System));
        Assert.That(rebuilt[5].Role, Is.EqualTo(ChatMessageRoles.Assistant));

        // Confirm summaries are inserted at the correct indices
        foreach (var idx in new[] { 1, 2, 3, 5 })
        {
            Assert.That(rebuilt[idx].GetMessageContent(), Does.StartWith("[Compressed summary]:"));
        }
    }
}
