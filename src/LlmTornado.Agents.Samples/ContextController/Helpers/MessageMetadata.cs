using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Represents the compression state of a message.
/// </summary>
public enum CompressionState
{
    /// <summary>
    /// Message has not been compressed
    /// </summary>
    Uncompressed = 0,

    /// <summary>
    /// Message has been compressed once
    /// </summary>
    Compressed = 1,

    /// <summary>
    /// Message has been re-compressed (compressed multiple times)
    /// </summary>
    ReCompressed = 2
}

/// <summary>
/// Metadata information about a chat message for compression tracking.
/// </summary>
public class MessageMetadata
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Current compression state of the message
    /// </summary>
    public CompressionState State { get; set; }

    /// <summary>
    /// Number of times this message has been compressed (0 = original)
    /// </summary>
    public int CompressionGeneration { get; set; }

    /// <summary>
    /// When the message was originally created/tracked
    /// </summary>
    public DateTime OriginalTimestamp { get; set; }

    /// <summary>
    /// Estimated token count for this message
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Whether this is a system message (should never be compressed)
    /// </summary>
    public bool IsSystemMessage { get; set; }

    /// <summary>
    /// Whether this message is considered "large" (>10k tokens by default)
    /// </summary>
    public bool IsLargeMessage => EstimatedTokens > 10000;
}

/// <summary>
/// Stores and manages metadata for chat messages to track compression state.
/// </summary>
/// <remarks>
/// This store keeps metadata in memory for all tracked messages. For very long-running
/// conversations (>100k messages), consider periodic cleanup using <see cref="RemoveWhere"/>
/// to remove metadata for messages no longer in use.
/// Memory usage: ~200 bytes per message, so 10,000 messages ? 2MB RAM.
/// </remarks>
public class MessageMetadataStore
{
    private readonly Dictionary<Guid, MessageMetadata> _metadata = new();
    private readonly object _lock = new();

    /// <summary>
    /// Tracks a new message in the metadata store.
    /// </summary>
    /// <param name="message">The message to track</param>
    /// <param name="state">Initial compression state (default: Uncompressed)</param>
    /// <remarks>
    /// If the message is already tracked (duplicate ID), this method does nothing.
    /// This is thread-safe and can be called from multiple threads.
    /// </remarks>
    public void Track(ChatMessage message, CompressionState state = CompressionState.Uncompressed)
    {
        if (message == null)
            return;

        Guid messageId = message.Id;
        
        lock (_lock)
        {
            if (_metadata.ContainsKey(messageId))
                return; // Already tracked

            var metadata = new MessageMetadata
            {
                MessageId = messageId,
                State = state,
                CompressionGeneration = 0,
                OriginalTimestamp = DateTime.UtcNow,
                EstimatedTokens = TokenEstimator.EstimateTokens(message),
                IsSystemMessage = message.Role == ChatMessageRoles.System
            };

            _metadata[messageId] = metadata;
        }
    }

    /// <summary>
    /// Retrieves metadata for a specific message.
    /// </summary>
    /// <param name="messageId">The message ID to look up</param>
    /// <returns>MessageMetadata if found, null otherwise</returns>
    public MessageMetadata? Get(Guid messageId)
    {
        lock (_lock)
        {
            return _metadata.TryGetValue(messageId, out var meta) ? meta : null;
        }
    }

    /// <summary>
    /// Updates the compression state of a message and increments its generation.
    /// </summary>
    /// <param name="messageId">The message ID to update</param>
    /// <param name="newState">The new compression state</param>
    /// <remarks>
    /// If the message is not found, this method does nothing.
    /// The compression generation is automatically incremented on each state update.
    /// </remarks>
    public void UpdateState(Guid messageId, CompressionState newState)
    {
        lock (_lock)
        {
            if (_metadata.TryGetValue(messageId, out var meta))
            {
                meta.State = newState;
                meta.CompressionGeneration++;
            }
        }
    }

    /// <summary>
    /// Gets messages with a specific compression state, ordered by age (oldest first).
    /// </summary>
    /// <param name="messages">The messages to filter</param>
    /// <param name="state">The compression state to filter by</param>
    /// <returns>List of messages matching the state, ordered oldest first</returns>
    public List<ChatMessage> GetOldestByState(List<ChatMessage> messages, CompressionState state)
    {
        if (messages == null || messages.Count == 0)
            return new List<ChatMessage>();

        lock (_lock)
        {
            return messages
                .Where(m => _metadata.TryGetValue(m.Id, out var meta) && meta.State == state)
                .OrderBy(m => _metadata[m.Id].OriginalTimestamp)
                .ToList();
        }
    }

    /// <summary>
    /// Gets all messages with a specific compression state.
    /// </summary>
    /// <param name="messages">The messages to filter</param>
    /// <param name="state">The compression state to filter by</param>
    /// <returns>List of messages matching the state</returns>
    public List<ChatMessage> GetByState(List<ChatMessage> messages, CompressionState state)
    {
        if (messages == null || messages.Count == 0)
            return new List<ChatMessage>();

        lock (_lock)
        {
            return messages
                .Where(m => _metadata.TryGetValue(m.Id, out var meta) && meta.State == state)
                .ToList();
        }
    }

    /// <summary>
    /// Gets all large messages (messages exceeding token threshold).
    /// </summary>
    /// <param name="messages">The messages to filter</param>
    /// <param name="threshold">Token threshold (default: 10000)</param>
    /// <returns>List of large messages</returns>
    public List<ChatMessage> GetLargeMessages(List<ChatMessage> messages, int threshold = 10000)
    {
        if (messages == null || messages.Count == 0)
            return new List<ChatMessage>();

        lock (_lock)
        {
            return messages
                .Where(m => _metadata.TryGetValue(m.Id, out var meta) && 
                           !meta.IsSystemMessage &&
                           meta.EstimatedTokens > threshold)
                .ToList();
        }
    }

    /// <summary>
    /// Gets the total estimated tokens for messages in a specific state.
    /// </summary>
    /// <param name="messages">The messages to sum</param>
    /// <param name="state">The compression state to filter by</param>
    /// <returns>Total estimated tokens</returns>
    public int GetTotalTokensByState(List<ChatMessage> messages, CompressionState state)
    {
        if (messages == null || messages.Count == 0)
            return 0;

        lock (_lock)
        {
            return messages
                .Where(m => _metadata.TryGetValue(m.Id, out var meta) && meta.State == state)
                .Sum(m => _metadata[m.Id].EstimatedTokens);
        }
    }

    /// <summary>
    /// Removes metadata entries that match a predicate. Useful for cleaning up old message metadata.
    /// </summary>
    /// <param name="predicate">Predicate to determine which message IDs to remove</param>
    /// <returns>Number of entries removed</returns>
    /// <remarks>
    /// This is useful for memory optimization in long-running conversations.
    /// Example: Remove metadata for messages no longer in the conversation:
    /// <code>
    /// var currentIds = conversation.Messages.Select(m => m.Id).ToHashSet();
    /// int removed = metadataStore.RemoveWhere(id => !currentIds.Contains(id));
    /// </code>
    /// </remarks>
    public int RemoveWhere(Func<Guid, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        lock (_lock)
        {
            var toRemove = _metadata.Keys.Where(predicate).ToList();
            foreach (var key in toRemove)
            {
                _metadata.Remove(key);
            }
            return toRemove.Count;
        }
    }

    /// <summary>
    /// Gets all tracked message IDs.
    /// </summary>
    /// <returns>Collection of all tracked message IDs</returns>
    /// <remarks>
    /// Useful for debugging or cleanup operations.
    /// </remarks>
    public IEnumerable<Guid> GetAllMessageIds()
    {
        lock (_lock)
        {
            return _metadata.Keys.ToList();
        }
    }

    /// <summary>
    /// Gets memory usage statistics for the metadata store.
    /// </summary>
    /// <returns>Estimated memory usage in bytes</returns>
    /// <remarks>
    /// Rough estimate: ~200 bytes per metadata entry.
    /// </remarks>
    public long GetEstimatedMemoryUsage()
    {
        lock (_lock)
        {
            // Rough estimate: 200 bytes per entry
            // (Guid=16 + enum=4 + int=4 + DateTime=8 + int=4 + bool=1 + overhead?163)
            return _metadata.Count * 200L;
        }
    }

    /// <summary>
    /// Clears all metadata (useful for testing or reset scenarios).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _metadata.Clear();
        }
    }

    /// <summary>
    /// Gets the count of tracked messages.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _metadata.Count;
            }
        }
    }
}
