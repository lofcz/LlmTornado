using System;
using System.Collections.Generic;
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
/// Analysis of compression needs for a message set.
/// </summary>
public class CompressionAnalysis
{
    public List<ChatMessage> LargeMessages { get; set; } = new();
    public List<ChatMessage> UncompressedMessages { get; set; } = new();
    public List<ChatMessage> CompressedMessages { get; set; } = new();
    public List<ChatMessage> SystemMessages { get; set; } = new();
    public bool NeedsUncompressedCompression { get; set; }
    public bool NeedsReCompression { get; set; }
    public double TargetUtilization { get; set; }
    public double ReCompressionTarget { get; set; }
}

/// <summary>
/// Advanced message summarizer that implements selective compression
/// based on context window utilization targets.
/// </summary>
public class ContextWindowMessageSummarizer : IMessagesSummarizer
{
    private readonly TornadoApi _client;
    private readonly ChatModel _model;
    private readonly MessageMetadataStore _metadataStore;

    public ContextWindowMessageSummarizer(
        TornadoApi client,
        ChatModel model,
        MessageMetadataStore metadataStore)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
    }

    public async Task<List<ChatMessage>> SummarizeMessages(
        List<ChatMessage> messages,
        MessageCompressionOptions options,
        CancellationToken token = default)
    {
        var analysis = AnalyzeCompressionNeeds(messages, options);

        List<ChatMessage> result = new();

        // Step 1: Handle large messages (>10k tokens)
        if (analysis.LargeMessages.Any())
        {
            var largeSummaries = await CompressLargeMessages(
                analysis.LargeMessages, options, token);
            result.AddRange(largeSummaries);
        }

        // Step 2: Handle uncompressed compression (60% threshold)
        if (analysis.NeedsUncompressedCompression)
        {
            var uncompressedSummaries = await CompressToTarget(
                analysis.UncompressedMessages,
                analysis.TargetUtilization,
                options,
                token);
            result.AddRange(uncompressedSummaries);
        }

        // Step 3: Handle re-compression (80% threshold)
        if (analysis.NeedsReCompression)
        {
            var recompressedSummaries = await ReCompressToTarget(
                analysis.CompressedMessages,
                analysis.ReCompressionTarget,
                options,
                token);
            result.AddRange(recompressedSummaries);
        }

        return result;
    }

    private async Task<List<ChatMessage>> CompressLargeMessages(
        List<ChatMessage> largeMessages,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        List<ChatMessage> summaries = new();

        foreach (var message in largeMessages)
        {
            var summary = await SummarizeSingleMessage(message, options, token);
            summaries.Add(summary);

            // Update metadata
            _metadataStore.UpdateState(message.Id, CompressionState.Compressed);
        }

        return summaries;
    }

    private async Task<List<ChatMessage>> CompressToTarget(
        List<ChatMessage> uncompressedMessages,
        double targetUtilization,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);
        int targetTokens = (int)(contextWindow * targetUtilization);

        // Sort by oldest first
        var sortedMessages = _metadataStore.GetOldestByState(
            uncompressedMessages, CompressionState.Uncompressed);

        List<ChatMessage> toCompress = new();
        int currentTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));

        // Select oldest messages until we reach target
        foreach (var message in sortedMessages)
        {
            if (currentTokens <= targetTokens)
                break;

            toCompress.Add(message);
            currentTokens -= TokenEstimator.EstimateTokens(message);
        }

        if (!toCompress.Any())
            return new List<ChatMessage>();

        // Group into chunks and compress
        var chunks = CreateChunks(toCompress, options.ChunkSize);
        var summaryTasks = chunks.Select(chunk =>
            SummarizeChunk(_client, chunk, options, token)).ToArray();
        var summaries = await Task.WhenAll(summaryTasks);

        List<ChatMessage> result = new();
        foreach (var summary in summaries)
        {
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var summaryMessage = new ChatMessage(
                    ChatMessageRoles.Assistant,
                    $"[Compressed summary]: {summary}");
                result.Add(summaryMessage);

                // Track as compressed
                _metadataStore.Track(summaryMessage, CompressionState.Compressed);
            }
        }

        // Update metadata for compressed messages
        foreach (var message in toCompress)
        {
            _metadataStore.UpdateState(message.Id, CompressionState.Compressed);
        }

        return result;
    }

    private async Task<List<ChatMessage>> ReCompressToTarget(
        List<ChatMessage> compressedMessages,
        double targetUtilization,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        int contextWindow = TokenEstimator.GetContextWindowSize(_model);
        int targetTokens = (int)(contextWindow * targetUtilization);

        // Sort by oldest first (including both Compressed and ReCompressed)
        var sortedMessages = _metadataStore.GetOldestByState(
            compressedMessages, CompressionState.Compressed)
            .Concat(_metadataStore.GetOldestByState(
                compressedMessages, CompressionState.ReCompressed))
            .OrderBy(m =>
            {
                var meta = _metadataStore.Get(m.Id);
                return meta?.OriginalTimestamp ?? DateTime.MaxValue;
            })
            .ToList();

        List<ChatMessage> toReCompress = new();
        int currentTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));

        // Select oldest compressed messages until we reach target
        foreach (var message in sortedMessages)
        {
            if (currentTokens <= targetTokens)
                break;

            toReCompress.Add(message);
            currentTokens -= TokenEstimator.EstimateTokens(message);
        }

        if (!toReCompress.Any())
            return new List<ChatMessage>();

        // Use more aggressive compression for re-compression
        var reCompressionOptions = new MessageCompressionOptions
        {
            ChunkSize = options.ChunkSize * 2, // Larger chunks for re-compression
            PreserveSystemmessages = true,
            CompressToolCallmessages = options.CompressToolCallmessages,
            SummaryModel = options.SummaryModel,
            SummaryPrompt = "Create an ultra-concise summary focusing only on absolutely critical information:",
            MaxSummaryTokens = options.MaxSummaryTokens / 2 // Half the tokens
        };

        var chunks = CreateChunks(toReCompress, reCompressionOptions.ChunkSize);
        var summaryTasks = chunks.Select(chunk =>
            SummarizeChunk(_client, chunk, reCompressionOptions, token)).ToArray();
        var summaries = await Task.WhenAll(summaryTasks);

        List<ChatMessage> result = new();
        foreach (var summary in summaries)
        {
            if (!string.IsNullOrWhiteSpace(summary))
            {
                var summaryMessage = new ChatMessage(
                    ChatMessageRoles.Assistant,
                    $"[Re-compressed summary]: {summary}");
                result.Add(summaryMessage);

                // Track as re-compressed
                _metadataStore.Track(summaryMessage, CompressionState.ReCompressed);
            }
        }

        // Update metadata for re-compressed messages
        foreach (var message in toReCompress)
        {
            _metadataStore.UpdateState(message.Id, CompressionState.ReCompressed);
        }

        return result;
    }

    private async Task<ChatMessage> SummarizeSingleMessage(
        ChatMessage message,
        MessageCompressionOptions options,
        CancellationToken token)
    {
        var conversation = _client.Chat.CreateConversation(options.SummaryModel);
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

    private List<List<ChatMessage>> CreateChunks(
        List<ChatMessage> messages,
        int chunkSize)
    {
        List<List<ChatMessage>> chunks = new();
        List<ChatMessage> currentChunk = new();
        int currentLength = 0;

        foreach (var message in messages)
        {
            int messageLength = message.GetMessageLength();

            if (currentLength + messageLength > chunkSize && currentChunk.Any())
            {
                chunks.Add(currentChunk);
                currentChunk = new();
                currentLength = 0;
            }

            currentChunk.Add(message);
            currentLength += messageLength;
        }

        if (currentChunk.Any())
        {
            chunks.Add(currentChunk);
        }

        return chunks;
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
            Conversation conversation = client.Chat.CreateConversation(options.SummaryModel);
            conversation.AddSystemMessage(options.SummaryPrompt);
            conversation.AddUserMessage(chunkText.ToString());
            conversation.RequestParameters.MaxTokens = options.MaxSummaryTokens;
            conversation.RequestParameters.Temperature = 0.3;
            conversation.RequestParameters.CancellationToken = token;
            RestDataOrException<ChatRichResponse> response = await conversation.GetResponseRichSafe();
            if(response.Exception != null)
            {
                Console.WriteLine($"Error during summarization: {response.Exception.Message}");
                return $"[{chunk.Count} messages from this conversation]";
            }
            else
            {
                if (response.Data == null)
                {
                    return $"[{chunk.Count} messages from this conversation]";
                }
            }
            ChatResult? result = response.Data.Result;
            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
        catch (Exception)
        {
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
        var uncompressedMessages = _metadataStore.GetOldestByState(messages, CompressionState.Uncompressed).ToList();

        int systemTokens = systemMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int compressedTokens = compressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int uncompressedTokens = uncompressedMessages.Sum(m => TokenEstimator.EstimateTokens(m));
        int totalTokens = systemTokens + compressedTokens + uncompressedTokens;

        double totalUtilization = TokenEstimator.CalculateUtilization(totalTokens, contextWindow);
        double compressedAndSystemUtilization = TokenEstimator.CalculateUtilization(
            systemTokens + compressedTokens, contextWindow);

        var largeMessages = uncompressedMessages
            .Where(m => TokenEstimator.EstimateTokens(m) > 10000)
            .ToList();

        return new CompressionAnalysis
        {
            LargeMessages = largeMessages,
            UncompressedMessages = uncompressedMessages.Except(largeMessages).ToList(),
            CompressedMessages = compressedMessages,
            SystemMessages = systemMessages,
            NeedsUncompressedCompression = totalUtilization >= 0.60,
            NeedsReCompression = compressedAndSystemUtilization >= 0.80,
            TargetUtilization = 0.40,
            ReCompressionTarget = 0.20
        };
    }
}
