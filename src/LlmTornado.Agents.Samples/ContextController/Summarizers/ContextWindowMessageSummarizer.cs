using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Advanced message summarizer that implements selective compression
/// based on context window utilization targets. Rebuilds the full
/// conversation while preserving original message order.
/// </summary>
public class ContextWindowMessageSummarizer : IMessagesSummarizer
{
    private readonly TornadoApi _client;
    private readonly ChatModel _model;
    private readonly MessageMetadataStore _metadataStore;
    private readonly SummarizationMetrics _metrics;
    private readonly bool _enableLogging;
    private readonly Action<string>? _logAction;
    private readonly ContextWindowCompressionOptions _compressionOptions;

    public ContextWindowMessageSummarizer(
        TornadoApi client,
        ChatModel model,
        MessageMetadataStore metadataStore,
        ContextWindowCompressionOptions compressionOptions,
        bool enableLogging = false,
        Action<string>? logAction = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        _metrics = new SummarizationMetrics();
        _enableLogging = enableLogging;
        _logAction = logAction;
        _compressionOptions = compressionOptions ?? throw new ArgumentNullException(nameof(compressionOptions));
    }

    /// <summary>
    /// Gets the current summarization metrics
    /// </summary>
    public SummarizationMetrics Metrics => _metrics;

    // Represents a contiguous span of original messages to be summarized into a single message
    private sealed class CompressionGroup
    {
        public int StartIndex { get; init; }
        public int EndIndex { get; init; }
        public List<ChatMessage> Messages { get; init; } = new();
        public CompressionState NewState { get; init; } // Compressed or ReCompressed
        public string? Summary { get; set; }
        public string SummaryPrefix => NewState == CompressionState.ReCompressed
            ? "[Re-compressed summary]: "
            : "[Compressed summary]: ";
    }

    public async Task<List<ChatMessage>> SummarizeMessages(
        List<ChatMessage> messages,
        MessageCompressionOptions options,
        CancellationToken token = default)
    {
        if (messages == null || messages.Count == 0)
            return new List<ChatMessage>();

        var sw = Stopwatch.StartNew();
        int tokensBefore = messages.Sum(m => TokenEstimator.EstimateTokens(m));
        Log($"[ContextWindowMessageSummarizer] Starting summarization for {messages.Count} messages ({tokensBefore:N0} tokens)");

        // First, analyze needs using current thresholds
        var analysis = AnalyzeCompressionNeeds(messages, options);

        // Select message ids for each operation, preserving system exclusions
        var selectedLarge = new HashSet<Guid>(analysis.LargeMessages.Select(m => m.Id));

        // Compute target-driven selections for uncompressed and recompression
        var selectedToCompress = new HashSet<Guid>();
        var selectedToRecompress = new HashSet<Guid>();

        // Initial compression selection (reduce uncompressed until target)
        if (analysis.NeedsUncompressedCompression && analysis.UncompressedMessages.Count > 0)
        {
            SelectToTarget(
                allMessages: messages,
                candidateMessages: analysis.UncompressedMessages.Where(m => !selectedLarge.Contains(m.Id)).ToList(),
                targetUtilization: analysis.TargetUtilization,
                out var chosen);
            foreach (var m in chosen) selectedToCompress.Add(m.Id);
        }

        // Re-compression selection (reduce compressed+system until re-compression target)
        if (analysis.NeedsReCompression && analysis.CompressedMessages.Count > 0)
        {
            SelectToTarget(
                allMessages: messages,
                candidateMessages: analysis.CompressedMessages.Where(m => !selectedLarge.Contains(m.Id) && !selectedToCompress.Contains(m.Id)).ToList(),
                targetUtilization: analysis.ReCompressionTarget,
                out var chosen);
            foreach (var m in chosen) selectedToRecompress.Add(m.Id);
        }

        // Build contiguous groups that respect original order for each selection
        var groups = new List<CompressionGroup>();

        // Large messages become single-message groups (treated as initial compression)
        foreach (var (msg, idx) in messages.Select((m, i) => (m, i)))
        {
            if (selectedLarge.Contains(msg.Id))
            {
                groups.Add(new CompressionGroup
                {
                    StartIndex = idx,
                    EndIndex = idx,
                    Messages = new List<ChatMessage> { msg },
                    NewState = CompressionState.Compressed
                });
            }
        }

        // Initial compression groups
        groups.AddRange(BuildGroups(messages, selectedToCompress, options.ChunkSize, CompressionState.Compressed));
        // Re-compression groups (use larger chunk size later when summarizing)
        groups.AddRange(BuildGroups(messages, selectedToRecompress, options.ChunkSize * 2, CompressionState.ReCompressed));

        if (groups.Count == 0)
        {
            Log("[ContextWindowMessageSummarizer] No groups selected for summarization. Returning original messages.");
            return new List<ChatMessage>(messages);
        }

        // Summarize groups. Use specialized behavior for single large messages.
        foreach (var group in groups)
        {
            if (token.IsCancellationRequested) break;

            if (group.Messages.Count == 1 && selectedLarge.Contains(group.Messages[0].Id))
            {
                // Single large message compression
                var sm = await SummarizeSingleMessage(group.Messages[0], options, token);
                group.Summary = sm.Content ?? $"[{group.Messages.Count} messages from this conversation]";
            }
            else
            {
                // Summarize chunk of messages
                var useOptions = group.NewState == CompressionState.ReCompressed
                    ? new MessageCompressionOptions
                    {
                        ChunkSize = Math.Max(options.ChunkSize, 1) * 2,
                        PreserveSystemmessages = true,
                        CompressToolCallmessages = options.CompressToolCallmessages,
                        SummaryModel = options.SummaryModel,
                        SummaryPrompt = "Create an ultra-concise summary focusing only on absolutely critical information:",
                        MaxSummaryTokens = Math.Max(1, options.MaxSummaryTokens / 2)
                    }
                    : options;

                var summary = await SummarizeChunk(_client, group.Messages, useOptions, token);
                group.Summary = summary;
            }
        }

        // Rebuild the full message list in original order: replace spans with single summary
        var rebuilt = new List<ChatMessage>(messages.Count);
        var groupsByStart = groups.GroupBy(g => g.StartIndex).ToDictionary(g => g.Key, g => g.ToList());
        int iPtr = 0;
        while (iPtr < messages.Count)
        {
            if (groupsByStart.TryGetValue(iPtr, out var startingGroups))
            {
                // If multiple groups start at the same index (shouldn't happen normally),
                // choose the one with the farthest EndIndex to avoid overlapping emissions.
                var best = startingGroups.OrderByDescending(g => g.EndIndex).First();

                var content = best.Summary;
                if (string.IsNullOrWhiteSpace(content))
                    content = $"[{best.Messages.Count} messages from this conversation]";

                var summaryMessage = new ChatMessage(ChatMessageRoles.Assistant, best.SummaryPrefix + content);
                rebuilt.Add(summaryMessage);

                // Track summary and archive originals
                _metadataStore.Track(summaryMessage, best.NewState);
                foreach (var om in best.Messages)
                {
                    _metadataStore.UpdateState(om.Id, CompressionState.Archived);
                }

                iPtr = best.EndIndex + 1; // Skip the entire span
                continue;
            }

            // Not part of any group, preserve original message
            rebuilt.Add(messages[iPtr]);
            iPtr++;
        }

        sw.Stop();
        int tokensAfter = rebuilt.Sum(m => TokenEstimator.EstimateTokens(m));

        // Record metrics
        string type = groups.Any(g => g.Messages.Count == 1 && selectedLarge.Contains(g.Messages[0].Id)) ? "large"
                    : groups.Any(g => g.NewState == CompressionState.Compressed) ? "uncompressed"
                    : "recompressed";

        _metrics.RecordSummarization(
            messages.Count,
            rebuilt.Count,
            tokensBefore,
            tokensAfter,
            sw.ElapsedMilliseconds,
            type);

        Log($"[ContextWindowMessageSummarizer] Summarization complete: {messages.Count} ? {rebuilt.Count} messages, {tokensBefore:N0} ? {tokensAfter:N0} tokens ({sw.ElapsedMilliseconds}ms)");

        return rebuilt;
    }

    // Build contiguous, order-preserving groups capped by chunkSize
    private static List<CompressionGroup> BuildGroups(
        List<ChatMessage> original,
        HashSet<Guid> selectedIds,
        int chunkSize,
        CompressionState state)
    {
        var groups = new List<CompressionGroup>();
        if (selectedIds == null || selectedIds.Count == 0)
            return groups;

        int i = 0;
        while (i < original.Count)
        {
            if (!selectedIds.Contains(original[i].Id))
            {
                i++;
                continue;
            }

            // Start a group at i and extend while contiguous selected and under chunk size
            int start = i;
            int currentLength = 0;
            var spanMessages = new List<ChatMessage>();

            while (i < original.Count && selectedIds.Contains(original[i].Id))
            {
                var msg = original[i];
                int msgLen = msg.GetMessageLength();

                if (spanMessages.Count > 0 && currentLength + msgLen > chunkSize)
                {
                    // Flush current span and start a new group
                    groups.Add(new CompressionGroup
                    {
                        StartIndex = start,
                        EndIndex = i - 1,
                        Messages = new List<ChatMessage>(spanMessages),
                        NewState = state
                    });

                    // Reset for new span starting at current index
                    start = i;
                    currentLength = 0;
                    spanMessages.Clear();
                }

                spanMessages.Add(msg);
                currentLength += msgLen;
                i++;

                // If next message is not selected, we must close the current span
                if (i < original.Count && !selectedIds.Contains(original[i].Id))
                {
                    groups.Add(new CompressionGroup
                    {
                        StartIndex = start,
                        EndIndex = i - 1,
                        Messages = new List<ChatMessage>(spanMessages),
                        NewState = state
                    });
                    spanMessages.Clear();
                    currentLength = 0;
                    break;
                }
            }

            // Close any remaining span at end
            if (spanMessages.Count > 0)
            {
                groups.Add(new CompressionGroup
                {
                    StartIndex = start,
                    EndIndex = i - 1,
                    Messages = new List<ChatMessage>(spanMessages),
                    NewState = state
                });
                spanMessages.Clear();
            }
        }

        return groups;
    }

    // Select oldest messages from candidates until uncompressed usage reaches target
    private void SelectToTarget(
        List<ChatMessage> allMessages,
        List<ChatMessage> candidateMessages,
        double targetUtilization,
        out List<ChatMessage> selected)
    {
        selected = new List<ChatMessage>();
        if (candidateMessages == null || candidateMessages.Count == 0)
            return;

        int contextWindow = TokenEstimator.GetContextWindowSize(_model);
        int targetTokens = (int)(contextWindow * targetUtilization);

        // Compute current token totals
        int systemTokens = allMessages.Where(m => m.Role == ChatMessageRoles.System).Sum(TokenEstimator.EstimateTokens);
        int compressedTokens = allMessages
            .Where(m => _metadataStore.Get(m.Id)?.State is CompressionState.Compressed or CompressionState.ReCompressed)
            .Sum(TokenEstimator.EstimateTokens);
        int uncompressedTokens = allMessages
            .Where(m => _metadataStore.Get(m.Id)?.State == CompressionState.Uncompressed && m.Role != ChatMessageRoles.System)
            .Sum(TokenEstimator.EstimateTokens);

        int currentUncompressed = uncompressedTokens;
        int allowedUncompressed = Math.Max(0, targetTokens - (systemTokens + compressedTokens));

        // Oldest first among candidates
        var sorted = _metadataStore.GetOldestByState(candidateMessages, CompressionState.Uncompressed)
            .Concat(_metadataStore.GetOldestByState(candidateMessages, CompressionState.Compressed)) // allow re-compress selection when passed in
            .ToList();

        foreach (var msg in sorted)
        {
            if (currentUncompressed <= allowedUncompressed)
                break;

            selected.Add(msg);
            currentUncompressed -= TokenEstimator.EstimateTokens(msg);
        }
    }

    private async Task<ChatMessage> SummarizeSingleMessage(
        ChatMessage message,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        var conversation = _client.Chat.CreateConversation(options.SummaryModel ?? _model);
        conversation.RequestParameters.Messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRoles.System,
                "Compress this large message while preserving all key information:"),
            message
        };
        conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
        conversation.RequestParameters.Temperature = 0.3;
        conversation.RequestParameters.CancellationToken = token;

        var result = (await conversation.GetResponseRichSafe())?.Data?.Result;
        var content = result?.Choices?[0]?.Message?.Content ?? "[Compression failed]";

        return new ChatMessage(ChatMessageRoles.Assistant,
            $"[Large message compressed]: {content}");
    }

    private async Task<string> SummarizeChunk(
        TornadoApi client,
        List<ChatMessage> chunk,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        StringBuilder chunkText = new StringBuilder();

        foreach (var msg in chunk)
        {
            string roleStr = msg.Role switch
            {
                ChatMessageRoles.System => "System",
                ChatMessageRoles.User => "User",
                ChatMessageRoles.Assistant => "Assistant",
                ChatMessageRoles.Tool => "Tool",
                _ => "Unknown"
            };

            chunkText.AppendLine($"{roleStr}: {msg.GetMessageContent()}");
        }

        try
        {
            Log($"[ContextWindowMessageSummarizer] Summarizing chunk of {chunk.Count} messages ({chunkText.Length} characters)");

            Conversation conversation = client.Chat.CreateConversation(options.SummaryModel ?? _model);
            conversation.AddSystemMessage(options.SummaryPrompt);
            conversation.AddUserMessage(chunkText.ToString());
            conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
            conversation.RequestParameters.Temperature = 0.3;
            conversation.RequestParameters.CancellationToken = token;
            RestDataOrException<ChatRichResponse> response = await conversation.GetResponseRichSafe();
            if(response.Exception != null)
            {
                Log($"[ContextWindowMessageSummarizer] Error during summarization: {response.Exception.Message}");
                return $"[{chunk.Count} messages from this conversation]";
            }
            else
            {
                if (response.Data == null)
                {
                    Log($"[ContextWindowMessageSummarizer] No data in response");
                    return $"[{chunk.Count} messages from this conversation]";
                }
            }
            ChatResult? result = response.Data.Result;
            var summary = result?.Choices?[0]?.Message?.Content ?? string.Empty;
            
            Log($"[ContextWindowMessageSummarizer] Chunk summarized: {chunkText.Length} chars ? {summary.Length} chars");
            
            return summary;
        }
        catch (Exception ex)
        {
            Log($"[ContextWindowMessageSummarizer] Exception during summarization: {ex.Message}");
            return $"[{chunk.Count} messages from this conversation]";
        }
    }

    private CompressionAnalysis AnalyzeCompressionNeeds(
        List<ChatMessage> messages,
        MessageCompressionOptions options)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);

        var systemMessages = messages.Where(m => m.Role == ChatMessageRoles.System).ToList();
        var compressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Compressed)
            .Concat(_metadataStore.GetOldestByState(messages, CompressionState.ReCompressed))
            .ToList();
        var uncompressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Uncompressed)
            .Where(m => m.Role != ChatMessageRoles.System)
            .ToList();

        int systemTokens = systemMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int compressedTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int uncompressedTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int totalTokens = systemTokens + compressedTokens + uncompressedTokens;

        double totalUtilization = TokenEstimator.CalculateUtilization(totalTokens, contextWindow);
        double compressedAndSystemUtilization = TokenEstimator.CalculateUtilization(
            systemTokens + compressedTokens, contextWindow);

        var largeMessages = uncompressedMessages
            .Where(m => TokenEstimator.EstimateTokens(m) > (_compressionOptions.LargeMessageThreshold > 0 ? _compressionOptions.LargeMessageThreshold : 10000))
            .ToList();

        return new CompressionAnalysis
        {
            LargeMessages = largeMessages,
            UncompressedMessages = uncompressedMessages.Except(largeMessages).ToList(),
            CompressedMessages = compressedMessages,
            SystemMessages = systemMessages,
            NeedsUncompressedCompression = totalUtilization >= _compressionOptions.UncompressedCompressionThreshold,
            NeedsReCompression = compressedAndSystemUtilization >= _compressionOptions.CompressedReCompressionThreshold,
            TargetUtilization = _compressionOptions.TargetUtilization,
            ReCompressionTarget = _compressionOptions.ReCompressionTarget
        };
    }

    private void Log(string message)
    {
        if (_enableLogging)
        {
            if (_logAction != null)
            {
                _logAction(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}
