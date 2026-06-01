using System.Collections.Generic;
using System.Threading;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Compaction;

/// <summary>
/// Request for Anthropic server-side context compaction (beta).
/// Compaction is enabled via <see cref="ContextManagement"/> and uses the Messages API with the
/// <c>compact-2026-01-12</c> beta header.
/// </summary>
public class CompactionRequest : ISerializableRequest, IHeaderProvider
{
    /// <summary>
    /// Beta header required for compaction requests.
    /// </summary>
    public const string BetaHeader = "compact-2026-01-12";

    /// <summary>
    /// Creates a new, empty compaction request.
    /// </summary>
    public CompactionRequest()
    {
    }

    /// <summary>
    /// Creates a compaction request for the given model and messages.
    /// </summary>
    public CompactionRequest(ChatModel model, List<ChatMessage> messages)
    {
        Model = model;
        Messages = messages;
    }

    /// <summary>
    /// Creates a compaction request for the given model and user prompt.
    /// </summary>
    public CompactionRequest(ChatModel model, string userPrompt)
    {
        Model = model;
        Messages = [new ChatMessage(ChatMessageRoles.User, userPrompt)];
    }

    /// <summary>
    /// The model to use. Must support compaction (Claude Opus 4.6+ or Sonnet 4.6+).
    /// </summary>
    public ChatModel? Model { get; set; }

    /// <summary>
    /// Conversation messages to send. Include prior compaction blocks to continue from a summary.
    /// </summary>
    public List<ChatMessage>? Messages { get; set; }

    /// <summary>
    /// Maximum tokens to generate in the response. Defaults to 4096.
    /// </summary>
    public int? MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Optional system prompt.
    /// </summary>
    public string? System { get; set; }

    /// <summary>
    /// Context management configuration. When unset, a default <c>compact_20260112</c> edit is applied.
    /// </summary>
    public AnthropicContextManagement? ContextManagement { get; set; }

    /// <summary>
    /// Shortcut for setting the compaction token trigger threshold (minimum 50,000).
    /// Ignored when <see cref="ContextManagement"/> is set explicitly.
    /// </summary>
    public int? TriggerTokenThreshold { get; set; }

    /// <summary>
    /// Custom summarization instructions. Completely replaces the default prompt when provided.
    /// Ignored when <see cref="ContextManagement"/> is set explicitly.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// When true, the API pauses after generating the compaction summary with stop reason <c>compaction</c>.
    /// Ignored when <see cref="ContextManagement"/> is set explicitly.
    /// </summary>
    public bool? PauseAfterCompaction { get; set; }

    /// <summary>
    /// Optional tools to include in the request.
    /// </summary>
    public List<Tool>? Tools { get; set; }

    /// <summary>
    /// Whether to stream the response.
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Cancellation token for the outbound request.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Resolves the context management configuration for this request.
    /// </summary>
    public AnthropicContextManagement ResolveContextManagement()
    {
        if (ContextManagement is not null)
        {
            return ContextManagement;
        }

        AnthropicCompactionEdit edit = new AnthropicCompactionEdit();

        if (TriggerTokenThreshold is not null)
        {
            edit.Trigger = new AnthropicCompactionTrigger(TriggerTokenThreshold.Value);
        }

        if (Instructions is not null)
        {
            edit.Instructions = Instructions;
        }

        if (PauseAfterCompaction is not null)
        {
            edit.PauseAfterCompaction = PauseAfterCompaction;
        }

        return new AnthropicContextManagement
        {
            Edits = [edit]
        };
    }

    /// <summary>
    /// Converts this compaction request into an equivalent chat request.
    /// </summary>
    public ChatRequest ToChatRequest()
    {
        List<ChatMessage> messages = Messages is not null ? [..Messages] : [];

        if (!string.IsNullOrEmpty(System))
        {
            messages.Insert(0, new ChatMessage(ChatMessageRoles.System, System));
        }

        return new ChatRequest
        {
            Model = Model,
            Messages = messages,
            MaxTokens = MaxTokens ?? 4096,
            Tools = Tools,
            Stream = Stream,
            CancellationToken = CancellationToken,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    ContextManagement = ResolveContextManagement()
                }
            }
        };
    }

    /// <inheritdoc />
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return Serialize(provider, new RequestSerializeOptions());
    }

    /// <inheritdoc />
    public TornadoRequestContent Serialize(IEndpointProvider provider, RequestSerializeOptions options)
    {
        ChatRequest chatRequest = ToChatRequest();
        TornadoRequestContent content = chatRequest.Serialize(provider, options);
        return new TornadoRequestContent(content.Body, chatRequest.Model, content.Url, provider, CapabilityEndpoints.Compaction);
    }

    IEnumerable<string> IHeaderProvider.GetHeaders(LLmProviders provider)
    {
        if (provider is LLmProviders.Anthropic)
        {
            yield return BetaHeader;
        }
    }
}
