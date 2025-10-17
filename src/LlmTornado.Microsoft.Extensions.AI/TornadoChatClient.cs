using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using Microsoft.Extensions.AI;
using ChatMessage = LlmTornado.Chat.ChatMessage;

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Provides an <see cref="IChatClient"/> implementation for LlmTornado.
/// </summary>
public sealed class TornadoChatClient : IChatClient
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("LlmTornado.Microsoft.Extensions.AI.Chat");

    private readonly TornadoApi _api;
    private readonly ChatModel _defaultModel;
    private readonly ChatRequest? _defaultRequest;

    /// <summary>
    /// Initializes a new instance of <see cref="TornadoChatClient"/>.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use for chat operations.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    public TornadoChatClient(TornadoApi api, ChatModel defaultModel, ChatRequest? defaultRequest = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _defaultModel = defaultModel ?? throw new ArgumentNullException(nameof(defaultModel));
        _defaultRequest = defaultRequest;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TornadoChatClient"/>.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use for chat operations.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    public TornadoChatClient(TornadoApi api, string defaultModel, ChatRequest? defaultRequest = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _defaultModel = defaultModel ?? throw new ArgumentNullException(nameof(defaultModel));
        _defaultRequest = defaultRequest;
    }

    /// <inheritdoc />
    public ChatClientMetadata Metadata => new ChatClientMetadata(providerName: "LlmTornado", providerUri: new Uri("https://github.com/lofcz/LlmTornado"), defaultModelId: _defaultModel.ToString());

    /// <inheritdoc />
    public async Task<global::Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
        IEnumerable<global::Microsoft.Extensions.AI.ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("CompleteAsync");

        List<global::Microsoft.Extensions.AI.ChatMessage> msgs = chatMessages.ToList();
        
        activity?.SetTag("llm.model", _defaultModel.ToString());
        activity?.SetTag("llm.request.messages.count", msgs.Count);

        try
        {
            // Create request
            ChatRequest request = CreateRequest(options);
            request.Messages = msgs.Select(m => m.ToLlmTornado()).ToList();
            request.Stream = false;

            activity?.SetTag("llm.request.temperature", request.Temperature);
            activity?.SetTag("llm.request.max_tokens", request.MaxTokens);

            // Execute request
            ChatResult? result = await _api.Chat.CreateChatCompletion(request);

            if (result == null)
            {
                throw new InvalidOperationException("Chat completion returned null result.");
            }

            ChatResponse completion = result.ToChatCompletion();

            activity?.SetTag("llm.response.id", completion.ResponseId);
            activity?.SetTag("llm.response.finish_reason", completion.FinishReason?.ToString());
            activity?.SetTag("llm.usage.input_tokens", completion.Usage?.InputTokenCount);
            activity?.SetTag("llm.usage.output_tokens", completion.Usage?.OutputTokenCount);
            activity?.SetTag("llm.usage.total_tokens", completion.Usage?.TotalTokenCount);

            return completion;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<global::Microsoft.Extensions.AI.ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("CompleteStreamingAsync");

        List<global::Microsoft.Extensions.AI.ChatMessage> msgs = chatMessages.ToList();
        
        activity?.SetTag("llm.model", _defaultModel.ToString());
        activity?.SetTag("llm.request.messages.count", msgs.Count);
        activity?.SetTag("llm.streaming", true);

        // Create request
        ChatRequest request = CreateRequest(options);
        request.Messages = msgs.Select(m => m.ToLlmTornado()).ToList();
        request.Stream = true;

        activity?.SetTag("llm.request.temperature", request.Temperature);
        activity?.SetTag("llm.request.max_tokens", request.MaxTokens);

        int chunkIndex = 0;
        string accumulatedText = "";
        List<ToolCall> accumulatedToolCalls = [];

        // Execute streaming request
        await foreach (ChatResult? chunk in _api.Chat.StreamChatEnumerable(request).WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (chunk == null)
            {
                continue;
            }

            ChatChoice? choice = chunk.Choices?.FirstOrDefault();
            ChatMessage? delta = choice?.Delta;
            
            if (delta == null)
            {
                continue;
            }

            // Accumulate content
            if (!string.IsNullOrEmpty(delta.Content))
            {
                accumulatedText += delta.Content;
            }

            // Track tool calls
            if (delta.ToolCalls != null)
            {
                foreach (ToolCall toolCall in delta.ToolCalls)
                {
                    if (toolCall.Index.HasValue)
                    {
                        while (accumulatedToolCalls.Count <= toolCall.Index.Value)
                        {
                            accumulatedToolCalls.Add(new ToolCall());
                        }

                        ToolCall existing = accumulatedToolCalls[toolCall.Index.Value];
                        existing.Id ??= toolCall.Id;
                        
                        if (toolCall.FunctionCall != null)
                        {
                            existing.FunctionCall ??= new FunctionCall();
                            existing.FunctionCall.Name ??= toolCall.FunctionCall.Name;
                            existing.FunctionCall.Arguments = (existing.FunctionCall.Arguments ?? "") + (toolCall.FunctionCall.Arguments ?? "");
                        }
                    }
                }
            }

            // Create streaming update
            List<AIContent> contents = [];

            if (!string.IsNullOrEmpty(delta.Content))
            {
                contents.Add(new TextContent(delta.Content));
            }

            // Add tool calls if present
            if (delta.ToolCalls != null)
            {
                foreach (ToolCall toolCall in delta.ToolCalls)
                {
                    if (toolCall.FunctionCall != null && !string.IsNullOrEmpty(toolCall.FunctionCall.Name))
                    {
                        contents.Add(new FunctionCallContent(
                            toolCall.Id ?? Guid.NewGuid().ToString(),
                            toolCall.FunctionCall.Name,
                            toolCall.FunctionCall.Arguments != null
                                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionCall.Arguments)
                                : null));
                    }
                }
            }

            ChatRole role = delta.Role switch
            {
                ChatMessageRoles.User => ChatRole.User,
                ChatMessageRoles.Assistant => ChatRole.Assistant,
                ChatMessageRoles.System => ChatRole.System,
                ChatMessageRoles.Tool => ChatRole.Tool,
                _ => ChatRole.Assistant
            };

            ChatResponseUpdate update = new ChatResponseUpdate
            {
                ResponseId = chunk.Id,
                Contents = contents,
                Role = role,
                RawRepresentation = chunk
            };

            if (choice.FinishReason != null)
            {
                update.FinishReason = choice.FinishReason switch
                {
                    ChatMessageFinishReasons.EndTurn => ChatFinishReason.Stop,
                    ChatMessageFinishReasons.StopSequence => ChatFinishReason.Stop,
                    ChatMessageFinishReasons.Length => ChatFinishReason.Length,
                    ChatMessageFinishReasons.ToolCalls => ChatFinishReason.ToolCalls,
                    ChatMessageFinishReasons.ContentFilter => ChatFinishReason.ContentFilter,
                    _ => null
                };
            }

            chunkIndex++;
            yield return update;
        }

        activity?.SetTag("llm.response.chunks", chunkIndex);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(TornadoApi) ? _api : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // TornadoApi doesn't implement IDisposable, so nothing to dispose
    }

    /// <summary>
    /// Creates a ChatRequest from options.
    /// </summary>
    private ChatRequest CreateRequest(ChatOptions? options)
    {
        ChatRequest request = _defaultRequest != null ? new ChatRequest(_defaultRequest) : new ChatRequest();
        request.Model = _defaultModel;

        if (options != null)
        {
            options.ApplyToRequest(request);

            // Override model if specified in options
            if (!string.IsNullOrEmpty(options.ModelId))
            {
                request.Model = options.ModelId;
            }
        }

        return request;
    }
}
