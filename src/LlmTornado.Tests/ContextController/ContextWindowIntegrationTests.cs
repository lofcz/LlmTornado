using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Tests.ContextController;

/// <summary>
/// Integration tests for the complete Context Window Management Strategy.
/// These tests verify end-to-end compression workflows.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ContextWindowIntegrationTests
{
    private TornadoApi _client;
    private MessageMetadataStore _metadataStore;
    private ContextWindowCompressionStrategy _strategy;
    private ContextWindowMessageSummarizer _summarizer;
    private ChatModel _model;

    [SetUp]
    public void Setup()
    {
        // Note: These tests require valid API credentials
        // Set OPENAI_API_KEY environment variable to run
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration tests.");
        }

        _client = new TornadoApi(LLmProviders.OpenAi, apiKey);
        _metadataStore = new MessageMetadataStore();
        _model = ChatModel.OpenAi.Gpt35.Turbo; // Use cheaper model for testing
        _strategy = new ContextWindowCompressionStrategy(_model, _metadataStore, new ContextWindowCompressionOptions() { SummaryModel = _model});
        _summarizer = new ContextWindowMessageSummarizer(_client, _model, _metadataStore);
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task EndToEnd_LargeMessageCompression_WorksCorrectly()
    {
        // Arrange - Create a large message
        var largeMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 50000));
        _metadataStore.Track(largeMessage);
        var messages = new List<ChatMessage> { largeMessage };

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);
        Assert.That(shouldCompress, Is.True, "Should trigger compression for large message");

        var options = _strategy.GetCompressionOptions(messages);
        var summaries = await _summarizer.SummarizeMessages(messages, options);

        // Assert
        Assert.That(summaries, Is.Not.Empty);
        Assert.That(summaries.Count, Is.GreaterThan(0));
        
        // Verify the summary is smaller than the original
        int originalTokens = TokenEstimator.EstimateTokens(largeMessage);
        int summaryTokens = summaries.Sum(s => TokenEstimator.EstimateTokens(s));
        Assert.That(summaryTokens, Is.LessThan(originalTokens));
    }

    [Test]
    [Explicit("Requires API key and makes real API calls")]
    public async Task EndToEnd_ProgressiveCompression_WorksCorrectly()
    {
        // Arrange - Create a conversation that grows over time
        var messages = new List<ChatMessage>();

        // Add initial messages below threshold
        for (int i = 0; i < 5; i++)
        {
            var msg = new ChatMessage(ChatMessageRoles.User, $"Message {i}: " + new string('x', 1000));
            _metadataStore.Track(msg);
            messages.Add(msg);
        }

        // Verify no compression needed yet
        Assert.That(_strategy.ShouldCompress(messages), Is.False);

        // Add more messages to exceed 60% threshold
        for (int i = 5; i < 15; i++)
        {
            var msg = new ChatMessage(ChatMessageRoles.User, $"Message {i}: " + new string('x', 20000));
            _metadataStore.Track(msg);
            messages.Add(msg);
        }

        // Act - First compression
        bool shouldCompress1 = _strategy.ShouldCompress(messages);
        Assert.That(shouldCompress1, Is.True, "Should trigger first compression");

        var options1 = _strategy.GetCompressionOptions(messages);

        var summaries1 = await _summarizer.SummarizeMessages(messages, options1);

        // Assert first compression
        Assert.That(summaries1, Is.Not.Empty);
        
        // Update messages list to include summaries
        var compressedMessages = new List<ChatMessage>(summaries1);
        
        // Add system message (should be preserved)
        var systemMsg = new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant");
        _metadataStore.Track(systemMsg);
        compressedMessages.Add(systemMsg);

        // Verify compressed messages are tracked
        foreach (var summary in summaries1)
        {
            var metadata = _metadataStore.Get(summary.Id);
            Assert.That(metadata, Is.Not.Null);
        }
    }

    [Test]
    public void Workflow_EmptyConversation_DoesNotCrash()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() =>
        {
            bool shouldCompress = _strategy.ShouldCompress(messages);
            Assert.That(shouldCompress, Is.False);
        });
    }

    [Test]
    public void Workflow_SingleMessage_DoesNotTriggerCompression()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Hello");
        _metadataStore.Track(message);
        var messages = new List<ChatMessage> { message };

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.False);
    }

    [Test]
    public void Workflow_OnlySystemMessages_DoesNotTriggerCompression()
    {
        // Arrange
        var messages = new List<ChatMessage>();
        for (int i = 0; i < 10; i++)
        {
            var msg = new ChatMessage(ChatMessageRoles.System, $"System message {i}");
            _metadataStore.Track(msg);
            messages.Add(msg);
        }

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        // Even with many system messages, they should never trigger compression
        Assert.That(shouldCompress, Is.False);
    }

    [Test]
    public void Workflow_MetadataTracking_WorksThroughoutLifecycle()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "First message");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Second message");

        // Act - Track messages
        _metadataStore.Track(message1);
        _metadataStore.Track(message2);

        // Assert - Initially uncompressed
        Assert.That(_metadataStore.Get(message1.Id).State, Is.EqualTo(CompressionState.Uncompressed));
        Assert.That(_metadataStore.Get(message2.Id).State, Is.EqualTo(CompressionState.Uncompressed));

        // Act - Simulate compression
        _metadataStore.UpdateState(message1.Id, CompressionState.Compressed);

        // Assert - State updated
        Assert.That(_metadataStore.Get(message1.Id).State, Is.EqualTo(CompressionState.Compressed));
        Assert.That(_metadataStore.Get(message1.Id).CompressionGeneration, Is.EqualTo(1));

        // Act - Simulate re-compression
        _metadataStore.UpdateState(message1.Id, CompressionState.ReCompressed);

        // Assert - State updated again
        Assert.That(_metadataStore.Get(message1.Id).State, Is.EqualTo(CompressionState.ReCompressed));
        Assert.That(_metadataStore.Get(message1.Id).CompressionGeneration, Is.EqualTo(2));
    }

    [Test]
    public void Workflow_Analysis_ProvidesAccurateMetrics()
    {
        // Arrange
        var systemMsg = new ChatMessage(ChatMessageRoles.System, new string('a', 4000)); // 1000 tokens
        var uncompressedMsg1 = new ChatMessage(ChatMessageRoles.User, new string('a', 8000)); // 2000 tokens
        var compressedMsg = new ChatMessage(ChatMessageRoles.Assistant, new string('a', 4000)); // 1000 tokens

        _metadataStore.Track(systemMsg);
        _metadataStore.Track(uncompressedMsg1);
        _metadataStore.Track(compressedMsg, CompressionState.Compressed);

        var messages = new List<ChatMessage> { systemMsg, uncompressedMsg1, compressedMsg };

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);

        // Assert
        Assert.That(analysis.SystemTokens, Is.EqualTo(1000));
        Assert.That(analysis.UncompressedTokens, Is.EqualTo(2000));
        Assert.That(analysis.CompressedTokens, Is.EqualTo(1000));
        Assert.That(analysis.TotalTokens, Is.EqualTo(4000));
        
        // Context window for GPT-3.5-Turbo is 16385
        double expectedUtilization = 4000.0 / 16385.0;
        Assert.That(analysis.TotalUtilization, Is.EqualTo(expectedUtilization).Within(0.001));
    }

    [Test]
    public void Workflow_CompressionOptions_AdaptToScenario()
    {
        // Arrange - Scenario 1: Normal compression needed
        var messages1 = new List<ChatMessage>();
        for (int i = 0; i < 10; i++)
        {
            var msg = new ChatMessage(ChatMessageRoles.User, new string('a', 30000));
            _metadataStore.Track(msg);
            messages1.Add(msg);
        }

        // Act
        var options1 = _strategy.GetCompressionOptions(messages1);

        // Assert
        Assert.That(options1.PreserveSystemmessages, Is.True);
        Assert.That(options1.SummaryPrompt, Does.Not.Contain("ultra-concise"));

        // Arrange - Scenario 2: Re-compression needed
        var messages2 = new List<ChatMessage>();
        // Add system messages
        for (int i = 0; i < 5; i++)
        {
            var sysMsg = new ChatMessage(ChatMessageRoles.System, new string('a', 40000));
            _metadataStore.Track(sysMsg);
            messages2.Add(sysMsg);
        }
        // Add compressed messages
        for (int i = 0; i < 5; i++)
        {
            var compMsg = new ChatMessage(ChatMessageRoles.Assistant, new string('a', 44000));
            _metadataStore.Track(compMsg, CompressionState.Compressed);
            messages2.Add(compMsg);
        }

        // Act
        var analysis2 = _strategy.AnalyzeMessages(messages2);

        // Assert - Should indicate re-compression needed
        Assert.That(analysis2.CompressedAndSystemUtilization, Is.GreaterThan(0.80));
    }

    [TearDown]
    public void TearDown()
    {
        _metadataStore?.Clear();
    }
}
