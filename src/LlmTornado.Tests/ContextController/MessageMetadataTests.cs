using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Chat;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Tests.ContextController;

/// <summary>
/// Unit tests for MessageMetadata and MessageMetadataStore classes.
/// </summary>
[TestFixture]
public class MessageMetadataTests
{
    private MessageMetadataStore _store;

    [SetUp]
    public void Setup()
    {
        _store = new MessageMetadataStore();
    }

    [Test]
    public void Track_NewMessage_AddsToStore()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Test message");

        // Act
        _store.Track(message);

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata.MessageId, Is.EqualTo(message.Id));
        Assert.That(metadata.State, Is.EqualTo(CompressionState.Uncompressed));
    }

    [Test]
    public void Track_DuplicateMessage_DoesNotOverwrite()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Test message");
        _store.Track(message);
        _store.UpdateState(message.Id, CompressionState.Compressed);

        // Act
        _store.Track(message); // Try to track again

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata.State, Is.EqualTo(CompressionState.Compressed)); // Should remain Compressed
    }

    [Test]
    public void Track_SystemMessage_SetsIsSystemMessageFlag()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.System, "System message");

        // Act
        _store.Track(message);

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata.IsSystemMessage, Is.True);
    }

    [Test]
    public void Track_UserMessage_DoesNotSetIsSystemMessageFlag()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "User message");

        // Act
        _store.Track(message);

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata.IsSystemMessage, Is.False);
    }

    [Test]
    public void Track_WithSpecificState_UsesProvidedState()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Test message");

        // Act
        _store.Track(message, CompressionState.Compressed);

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata.State, Is.EqualTo(CompressionState.Compressed));
    }

    [Test]
    public void UpdateState_ExistingMessage_UpdatesStateAndIncrementsGeneration()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Test message");
        _store.Track(message);
        var originalGeneration = _store.Get(message.Id).CompressionGeneration;

        // Act
        _store.UpdateState(message.Id, CompressionState.Compressed);

        // Assert
        var metadata = _store.Get(message.Id);
        Assert.That(metadata.State, Is.EqualTo(CompressionState.Compressed));
        Assert.That(metadata.CompressionGeneration, Is.EqualTo(originalGeneration + 1));
    }

    [Test]
    public void UpdateState_NonExistentMessage_DoesNothing()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => _store.UpdateState(nonExistentId, CompressionState.Compressed));
    }

    [Test]
    public void GetOldestByState_ReturnsMessagesInCorrectOrder()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "Message 1");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Message 2");
        var message3 = new ChatMessage(ChatMessageRoles.User, "Message 3");

        _store.Track(message1);
        System.Threading.Thread.Sleep(10); // Ensure different timestamps
        _store.Track(message2);
        System.Threading.Thread.Sleep(10);
        _store.Track(message3);

        var allMessages = new List<ChatMessage> { message3, message1, message2 }; // Random order

        // Act
        var oldest = _store.GetOldestByState(allMessages, CompressionState.Uncompressed);

        // Assert
        Assert.That(oldest.Count, Is.EqualTo(3));
        Assert.That(oldest[0].Id, Is.EqualTo(message1.Id)); // Oldest first
        Assert.That(oldest[1].Id, Is.EqualTo(message2.Id));
        Assert.That(oldest[2].Id, Is.EqualTo(message3.Id)); // Newest last
    }

    [Test]
    public void GetOldestByState_FiltersCorrectly()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "Message 1");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Message 2");
        var message3 = new ChatMessage(ChatMessageRoles.User, "Message 3");

        _store.Track(message1, CompressionState.Uncompressed);
        _store.Track(message2, CompressionState.Compressed);
        _store.Track(message3, CompressionState.Uncompressed);

        var allMessages = new List<ChatMessage> { message1, message2, message3 };

        // Act
        var uncompressed = _store.GetOldestByState(allMessages, CompressionState.Uncompressed);

        // Assert
        Assert.That(uncompressed.Count, Is.EqualTo(2));
        Assert.That(uncompressed.Any(m => m.Id == message1.Id), Is.True);
        Assert.That(uncompressed.Any(m => m.Id == message3.Id), Is.True);
        Assert.That(uncompressed.Any(m => m.Id == message2.Id), Is.False);
    }

    [Test]
    public void GetByState_ReturnsAllMatchingMessages()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "Message 1");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Message 2");
        var message3 = new ChatMessage(ChatMessageRoles.User, "Message 3");

        _store.Track(message1, CompressionState.Compressed);
        _store.Track(message2, CompressionState.Compressed);
        _store.Track(message3, CompressionState.Uncompressed);

        var allMessages = new List<ChatMessage> { message1, message2, message3 };

        // Act
        var compressed = _store.GetByState(allMessages, CompressionState.Compressed);

        // Assert
        Assert.That(compressed.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetLargeMessages_ReturnsMessagesAboveThreshold()
    {
        // Arrange
        var smallMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 1000)); // 250 tokens
        var largeMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 50000)); // 12500 tokens

        _store.Track(smallMessage);
        _store.Track(largeMessage);

        var allMessages = new List<ChatMessage> { smallMessage, largeMessage };

        // Act
        var largeMessages = _store.GetLargeMessages(allMessages, 10000);

        // Assert
        Assert.That(largeMessages.Count, Is.EqualTo(1));
        Assert.That(largeMessages[0].Id, Is.EqualTo(largeMessage.Id));
    }

    [Test]
    public void GetLargeMessages_ExcludesSystemMessages()
    {
        // Arrange
        var systemMessage = new ChatMessage(ChatMessageRoles.System, new string('a', 50000)); // Large but system
        var userMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 50000)); // Large user message

        _store.Track(systemMessage);
        _store.Track(userMessage);

        var allMessages = new List<ChatMessage> { systemMessage, userMessage };

        // Act
        var largeMessages = _store.GetLargeMessages(allMessages, 10000);

        // Assert
        Assert.That(largeMessages.Count, Is.EqualTo(1));
        Assert.That(largeMessages[0].Id, Is.EqualTo(userMessage.Id));
    }

    [Test]
    public void GetTotalTokensByState_ReturnsCorrectSum()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, new string('a', 4000)); // 1000 tokens
        var message2 = new ChatMessage(ChatMessageRoles.User, new string('a', 8000)); // 2000 tokens
        var message3 = new ChatMessage(ChatMessageRoles.User, new string('a', 4000)); // 1000 tokens

        _store.Track(message1, CompressionState.Compressed);
        _store.Track(message2, CompressionState.Compressed);
        _store.Track(message3, CompressionState.Uncompressed);

        var allMessages = new List<ChatMessage> { message1, message2, message3 };

        // Act
        int compressedTokens = _store.GetTotalTokensByState(allMessages, CompressionState.Compressed);

        // Assert
        Assert.That(compressedTokens, Is.EqualTo(3000)); // 1000 + 2000
    }

    [Test]
    public void Clear_RemovesAllMetadata()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "Message 1");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Message 2");

        _store.Track(message1);
        _store.Track(message2);

        // Act
        _store.Clear();

        // Assert
        Assert.That(_store.Count, Is.EqualTo(0));
        Assert.That(_store.Get(message1.Id), Is.Null);
        Assert.That(_store.Get(message2.Id), Is.Null);
    }

    [Test]
    public void Count_ReturnsCorrectNumber()
    {
        // Arrange
        var message1 = new ChatMessage(ChatMessageRoles.User, "Message 1");
        var message2 = new ChatMessage(ChatMessageRoles.User, "Message 2");
        var message3 = new ChatMessage(ChatMessageRoles.User, "Message 3");

        // Act & Assert
        Assert.That(_store.Count, Is.EqualTo(0));

        _store.Track(message1);
        Assert.That(_store.Count, Is.EqualTo(1));

        _store.Track(message2);
        Assert.That(_store.Count, Is.EqualTo(2));

        _store.Track(message3);
        Assert.That(_store.Count, Is.EqualTo(3));
    }

    [Test]
    public void MessageMetadata_IsLargeMessage_ReturnsTrueForLargeMessages()
    {
        // Arrange
        var largeMessage = new ChatMessage(ChatMessageRoles.User, new string('a', 50000)); // 12500 tokens
        _store.Track(largeMessage);

        // Act
        var metadata = _store.Get(largeMessage.Id);

        // Assert
        Assert.That(metadata.IsLargeMessage, Is.True);
    }

    [Test]
    public void MessageMetadata_IsLargeMessage_ReturnsFalseForSmallMessages()
    {
        // Arrange
        var smallMessage = new ChatMessage(ChatMessageRoles.User, "Small message");
        _store.Track(smallMessage);

        // Act
        var metadata = _store.Get(smallMessage.Id);

        // Assert
        Assert.That(metadata.IsLargeMessage, Is.False);
    }

    [Test]
    public void Track_NullMessage_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _store.Track(null));
    }

    [Test]
    public void GetOldestByState_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<ChatMessage>();

        // Act
        var result = _store.GetOldestByState(emptyList, CompressionState.Uncompressed);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetOldestByState_NullList_ReturnsEmptyList()
    {
        // Act
        var result = _store.GetOldestByState(null, CompressionState.Uncompressed);

        // Assert
        Assert.That(result, Is.Empty);
    }
}
