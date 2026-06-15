using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Compaction;

/// <summary>
/// Result from an Anthropic compaction-enabled Messages API call.
/// </summary>
public class CompactionResult : ApiResultBase
{
    /// <summary>
    /// The identifier of the result.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The model used for the request.
    /// </summary>
    [JsonProperty("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Why the model stopped generating.
    /// </summary>
    [JsonProperty("stop_reason")]
    public ChatMessageFinishReasons StopReason { get; set; }

    /// <summary>
    /// Whether compaction was triggered and the response paused after the summary.
    /// </summary>
    [JsonIgnore]
    public bool WasCompacted => StopReason is ChatMessageFinishReasons.Compaction;

    /// <summary>
    /// Server-generated compaction summary content, if present.
    /// </summary>
    [JsonIgnore]
    public string? CompactionContent { get; set; }

    /// <summary>
    /// Opaque metadata from prior compaction, to be round-tripped verbatim on subsequent requests.
    /// </summary>
    [JsonIgnore]
    public string? EncryptedContent { get; set; }

    /// <summary>
    /// Assistant message content blocks to append to the conversation history.
    /// Includes compaction blocks when present.
    /// </summary>
    [JsonIgnore]
    public ChatMessage? AssistantMessage { get; set; }

    /// <summary>
    /// Plaintext assistant response, excluding compaction blocks.
    /// </summary>
    [JsonIgnore]
    public string? Text { get; set; }

    /// <summary>
    /// Token usage for the request.
    /// </summary>
    [JsonProperty("usage")]
    public ChatUsage? Usage { get; set; }

    /// <summary>
    /// Raw chat result returned by the provider.
    /// </summary>
    [JsonIgnore]
    public ChatResult? ChatResult { get; set; }

    /// <summary>
    /// Raw JSON response from the API.
    /// </summary>
    [JsonIgnore]
    public string? RawResponse { get; set; }

    internal static CompactionResult? Deserialize(LLmProviders provider, string jsonData, string? postData, object? requestObject)
    {
        ChatResult? chatResult = ChatResult.Deserialize(provider, jsonData, postData, requestObject);

        if (chatResult is null)
        {
            return null;
        }

        return FromChatResult(chatResult, jsonData);
    }

    /// <summary>
    /// Creates a compaction result from a chat result.
    /// </summary>
    public static CompactionResult FromChatResult(ChatResult chatResult, string? rawResponse = null)
    {
        ChatChoice? primaryChoice = chatResult.Choices?.FirstOrDefault();
        ChatMessage? message = primaryChoice?.Message;
        List<ChatMessagePart>? parts = message?.Parts;

        ChatMessagePart? compactionPart = parts?.FirstOrDefault(x => x.Type is ChatMessageTypes.Compaction);
        string? text = parts?.Where(x => x.Type is ChatMessageTypes.Text)
            .Select(x => x.Text)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

        if (string.IsNullOrEmpty(text))
        {
            text = message?.Content;
        }

        ChatMessageFinishReasons stopReason = primaryChoice?.FinishReason ?? ChatMessageFinishReasons.Unknown;

        return new CompactionResult
        {
            Provider = chatResult.Provider,
            Id = chatResult.Id,
            StopReason = stopReason,
            CompactionContent = compactionPart?.Text,
            EncryptedContent = compactionPart?.EncryptedContent,
            AssistantMessage = message,
            Text = text,
            Usage = chatResult.Usage,
            ChatResult = chatResult,
            RawResponse = rawResponse ?? chatResult.RawResponse
        };
    }
}
