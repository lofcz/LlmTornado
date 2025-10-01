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

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Provides an <see cref="IChatClient"/> implementation for LlmTornado.
/// </summary>
public sealed class TornadoChatClient : IChatClient
{
    private static readonly ActivitySource ActivitySource = new("LlmTornado.Microsoft.Extensions.AI.Chat");

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
    public ChatClientMetadata Metadata => new(
        providerName: "LlmTornado",
        providerUri: new Uri("https://github.com/lofcz/LlmTornado"),
        modelId: _defaultModel.ToString());

    /// <inheritdoc />
    public async Task<ChatCompletion> CompleteAsync(
        IList<global::Microsoft.Extensions.AI.ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("CompleteAsync");

        activity?.SetTag("llm.model", _defaultModel.ToString());
        activity?.SetTag("llm.request.messages.count", chatMessages.Count);

        try
        {
            // Create request
            var request = CreateRequest(options);
            request.Messages = chatMessages.Select(m => m.ToLlmTornado()).ToList();
            request.Stream = false;

            activity?.SetTag("llm.request.temperature", request.Temperature);
            activity?.SetTag("llm.request.max_tokens", request.MaxTokens);

            // Execute request
            var result = await _api.Chat.CreateChatCompletion(request);

            if (result == null)
            {
                throw new InvalidOperationException("Chat completion returned null result.");
            }

            var completion = result.ToChatCompletion();

            activity?.SetTag("llm.response.id", completion.CompletionId);
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
    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<global::Microsoft.Extensions.AI.ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("CompleteStreamingAsync");

        activity?.SetTag("llm.model", _defaultModel.ToString());
        activity?.SetTag("llm.request.messages.count", chatMessages.Count);
        activity?.SetTag("llm.streaming", true);

        // Create request
        var request = CreateRequest(options);
        request.Messages = chatMessages.Select(m => m.ToLlmTornado()).ToList();
        request.Stream = true;

        activity?.SetTag("llm.request.temperature", request.Temperature);
        activity?.SetTag("llm.request.max_tokens", request.MaxTokens);

        int chunkIndex = 0;
        var accumulatedText = "";
        var accumulatedToolCalls = new List<LlmTornado.ChatFunctions.ToolCall>();

        // Execute streaming request
        await foreach (var chunk in _api.Chat.StreamChatEnumerable(request))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (chunk == null)
            {
                continue;
            }

            var choice = chunk.Choices?.FirstOrDefault();
            if (choice == null)
            {
                continue;
            }

            var delta = choice.Delta;
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
                foreach (var toolCall in delta.ToolCalls)
                {
                    if (toolCall.Index.HasValue)
                    {
                        while (accumulatedToolCalls.Count <= toolCall.Index.Value)
                        {
                            accumulatedToolCalls.Add(new LlmTornado.ChatFunctions.ToolCall());
                        }

                        var existing = accumulatedToolCalls[toolCall.Index.Value];
                        existing.Id ??= toolCall.Id;
                        if (toolCall.FunctionCall != null)
                        {
                            existing.FunctionCall ??= new LlmTornado.ChatFunctions.FunctionCall();
                            existing.FunctionCall.Name ??= toolCall.FunctionCall.Name;
                            existing.FunctionCall.Arguments = (existing.FunctionCall.Arguments ?? "") + (toolCall.FunctionCall.Arguments ?? "");
                        }
                    }
                }
            }

            // Create streaming update
            var contents = new List<AIContent>();

            if (!string.IsNullOrEmpty(delta.Content))
            {
                contents.Add(new TextContent(delta.Content));
            }

            // Add tool calls if present
            if (delta.ToolCalls != null)
            {
                foreach (var toolCall in delta.ToolCalls)
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

            var role = delta.Role switch
            {
                Code.ChatMessageRoles.User => ChatRole.User,
                Code.ChatMessageRoles.Assistant => ChatRole.Assistant,
                Code.ChatMessageRoles.System => ChatRole.System,
                Code.ChatMessageRoles.Tool => ChatRole.Tool,
                _ => ChatRole.Assistant
            };

            var update = new StreamingChatCompletionUpdate
            {
                CompletionId = chunk.Id,
                Contents = contents,
                Role = role,
                RawRepresentation = chunk
            };

            if (choice.FinishReason != null)
            {
                update.FinishReason = choice.FinishReason switch
                {
                    Code.ChatMessageFinishReasons.EndTurn => ChatFinishReason.Stop,
                    Code.ChatMessageFinishReasons.StopSequence => ChatFinishReason.Stop,
                    Code.ChatMessageFinishReasons.Length => ChatFinishReason.Length,
                    Code.ChatMessageFinishReasons.ToolCalls => ChatFinishReason.ToolCalls,
                    Code.ChatMessageFinishReasons.ContentFilter => ChatFinishReason.ContentFilter,
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
        if (serviceType == typeof(TornadoApi))
        {
            return _api;
        }

        return null;
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
        var request = _defaultRequest != null ? new ChatRequest(_defaultRequest) : new ChatRequest();
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
