using System.Collections.Generic;
using System.Linq;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Tests.ContextController;

/// <summary>
/// Unit tests for ContextWindowCompressionStrategy class.
/// </summary>
[TestFixture]
public class ContextWindowCompressionStrategyTests
{
    private MessageMetadataStore _metadataStore;
    private ContextWindowCompressionStrategy _strategy;
    private ChatModel _model;

    [SetUp]
    public void Setup()
    {
        _metadataStore = new MessageMetadataStore();
        _model = ChatModel.OpenAi.Gpt41.V41Mini; // 128k context window
        _strategy = new ContextWindowCompressionStrategy(_model, _metadataStore);
    }

    [Test]
    public void ShouldCompress_WithLargeMessage_ReturnsTrue()
    {
        // Arrange - Create a message with >10k tokens (40k+ characters)
        var largeMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 50000));
        _metadataStore.Track(largeMessage);
        var messages = new List<ChatMessage> { largeMessage };

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.True);
    }

    [Test]
    public void ShouldCompress_WithTotalUtilizationAbove60Percent_ReturnsTrue()
    {
        // Arrange - Gpt41.V41Mini has 1M context window
        // Create messages that exceed 60% of 1M context (>600k tokens)
        var messages = new List<ChatMessage>();
        // Each message ~4k tokens (16k characters)
        for (int i = 0; i < 160; i++) // 160 * 4k = 640k tokens (64% of 1M)
        {
            var msg = new ChatMessage(ChatMessageRoles.User, new string('a', 16000));
            _metadataStore.Track(msg);
            messages.Add(msg);
        }
        // Total: ~640k tokens (64% of 1M) - safely above 60% threshold

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.True);
    }

    [Test]
    public void ShouldCompress_WithCompressedAndSystemAbove80Percent_ReturnsTrue()
    {
        // Arrange - Gpt41.V41Mini has 1M context window
        // Need compressed + system >80% of 1M = >800k tokens
        var messages = new List<ChatMessage>();
        
        // Add system messages (~400k tokens)
        for (int i = 0; i < 100; i++)
        {
            var sysMsg = new ChatMessage(ChatMessageRoles.System, new string('a', 16000)); // 4k tokens each
            _metadataStore.Track(sysMsg);
            messages.Add(sysMsg);
        }

        // Add compressed messages (~450k tokens)
        for (int i = 0; i < 113; i++)
        {
            var compMsg = new ChatMessage(ChatMessageRoles.Assistant, new string('a', 16000)); // 4k tokens each
            _metadataStore.Track(compMsg, CompressionState.Compressed);
            messages.Add(compMsg);
        }
        // Total compressed+system: ~852k tokens (85.2% of 1M) - safely above 80% threshold

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.True);
    }

    [Test]
    public void ShouldCompress_BelowAllThresholds_ReturnsFalse()
    {
        // Arrange - Small conversation
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant"),
            new ChatMessage(ChatMessageRoles.User, "Hello"),
            new ChatMessage(ChatMessageRoles.Assistant, "Hi there!")
        };

        foreach (var msg in messages)
        {
            _metadataStore.Track(msg);
        }

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.False);
    }

    [Test]
    public void ShouldCompress_EmptyMessages_ReturnsFalse()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act
        bool shouldCompress = _strategy.ShouldCompress(messages);

        // Assert
        Assert.That(shouldCompress, Is.False);
    }

    [Test]
    public void AnalyzeMessages_CategorizesMessagesCorrectly()
    {
        // Arrange
        var systemMsg = new ChatMessage(ChatMessageRoles.System, "System");
        var uncompressedMsg = new ChatMessage(ChatMessageRoles.User, "Uncompressed");
        var compressedMsg = new ChatMessage(ChatMessageRoles.Assistant, "Compressed");
        var largeMsg = new ChatMessage(ChatMessageRoles.User, new string('a', 50000));

        _metadataStore.Track(systemMsg);
        _metadataStore.Track(uncompressedMsg);
        _metadataStore.Track(compressedMsg, CompressionState.Compressed);
        _metadataStore.Track(largeMsg);

        var messages = new List<ChatMessage> { systemMsg, uncompressedMsg, compressedMsg, largeMsg };

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);

        // Assert
        Assert.That(analysis.SystemMessages.Count, Is.EqualTo(1));
        Assert.That(analysis.CompressedMessages.Count, Is.EqualTo(1));
        Assert.That(analysis.UncompressedMessages.Count, Is.EqualTo(2));
        Assert.That(analysis.HasLargeMessages, Is.True);
    }

    [Test]
    public void AnalyzeMessages_CalculatesTokensCorrectly()
    {
        // Arrange
        var systemMsg = new ChatMessage(ChatMessageRoles.System, new string('a', 4000)); // 1000 tokens
        var userMsg = new ChatMessage(ChatMessageRoles.User, new string('a', 8000)); // 2000 tokens

        _metadataStore.Track(systemMsg);
        _metadataStore.Track(userMsg);

        var messages = new List<ChatMessage> { systemMsg, userMsg };

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);

        // Assert
        Assert.That(analysis.SystemTokens, Is.EqualTo(1000));
        Assert.That(analysis.UncompressedTokens, Is.EqualTo(2000));
        Assert.That(analysis.TotalTokens, Is.EqualTo(3000));
    }

    [Test]
    public void AnalyzeMessages_CalculatesUtilizationCorrectly()
    {
        // Arrange - Gpt41.V41Mini has 1M context window
        // Create messages totaling 500k tokens for 50% utilization
        var messages = new List<ChatMessage>();
        for (int i = 0; i < 125; i++) // 125 messages * 4000 tokens = 500k tokens
        {
            var msg = new ChatMessage(ChatMessageRoles.User, new string('a', 16000)); // 4k tokens each
            _metadataStore.Track(msg);
            messages.Add(msg);
        }

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);

        // Assert - Should be approximately 50% of 1M context window
        Assert.That(analysis.TotalUtilization, Is.EqualTo(0.5).Within(0.01));
    }

    [Test]
    public void GetCompressionOptions_ReturnsCorrectOptions()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRoles.User, "Test message")
        };
        _metadataStore.Track(messages[0]);

        // Act
        var options = _strategy.GetCompressionOptions(messages);

        // Assert
        Assert.That(options, Is.Not.Null);
        Assert.That(options.PreserveSystemmessages, Is.True);
        Assert.That(options.ChunkSize, Is.GreaterThan(0));
    }

    [Test]
    public void GetCompressionOptions_UsesInitialPromptForUncompressedCompression()
    {
        // Arrange - Total utilization at 60%
        var messages = new List<ChatMessage>();
        for (int i = 0; i < 10; i++)
        {
            var msg = new ChatMessage(ChatMessageRoles.User, new string('a', 30000)); // ~7.7k tokens each
            _metadataStore.Track(msg);
            messages.Add(msg);
        }

        // Act
        var options = _strategy.GetCompressionOptions(messages);

        // Assert
        Assert.That(options.SummaryPrompt, Does.Contain("Summarize"));
        Assert.That(options.SummaryPrompt, Does.Not.Contain("ultra-concise"));
    }

    [Test]
    public void Constructor_WithNullModel_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() =>
            new ContextWindowCompressionStrategy(null, _metadataStore));
    }

    [Test]
    public void Constructor_WithNullMetadataStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() =>
            new ContextWindowCompressionStrategy(_model, null));
    }

    [Test]
    public void Constructor_WithCustomOptions_UsesProvidedOptions()
    {
        // Arrange
        var customOptions = new ContextWindowCompressionOptions
        {
            TargetUtilization = 0.30,
            UncompressedCompressionThreshold = 0.70,
            LargeMessageThreshold = 20000
        };

        // Act
        var strategy = new ContextWindowCompressionStrategy(_model, _metadataStore, customOptions);

        // Assert
        Assert.That(strategy.Options.TargetUtilization, Is.EqualTo(0.30));
        Assert.That(strategy.Options.UncompressedCompressionThreshold, Is.EqualTo(0.70));
        Assert.That(strategy.Options.LargeMessageThreshold, Is.EqualTo(20000));
    }

    [Test]
    public void AnalyzeMessages_IncludesReCompressedInCompressedCategory()
    {
        // Arrange
        var compressedMsg = new ChatMessage(ChatMessageRoles.Assistant, "Compressed");
        var recompressedMsg = new ChatMessage(ChatMessageRoles.Assistant, "ReCompressed");

        _metadataStore.Track(compressedMsg, CompressionState.Compressed);
        _metadataStore.Track(recompressedMsg, CompressionState.ReCompressed);

        var messages = new List<ChatMessage> { compressedMsg, recompressedMsg };

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);

        // Assert
        Assert.That(analysis.CompressedMessages.Count, Is.EqualTo(2));
        Assert.That(analysis.CompressedMessages.Any(m => m.Id == compressedMsg.Id), Is.True);
        Assert.That(analysis.CompressedMessages.Any(m => m.Id == recompressedMsg.Id), Is.True);
    }

    [Test]
    public void AnalyzeMessages_ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Test");
        _metadataStore.Track(message);
        var messages = new List<ChatMessage> { message };

        // Act
        var analysis = _strategy.AnalyzeMessages(messages);
        string output = analysis.ToString();

        // Assert
        Assert.That(output, Does.Contain("Context Window Analysis"));
        Assert.That(output, Does.Contain("Total Window"));
        Assert.That(output, Does.Contain("tokens"));
    }
}
