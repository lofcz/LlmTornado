using LlmTornado.Agents.API.Hubs; // added for IStreamingEventService
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo.ExampleAgents;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using static LlmTornado.Demo.AgentOrchestrationRuntimeDemo;

namespace LlmTornado.Agents.API.Services;

/// <summary>
/// Interface for managing ChatRuntime instances
/// </summary>
public interface IChatRuntimeService
{
    Task<string> CreateRuntimeAsync(string configurationType, string agentName, string instructions, bool enableStreaming);
    ChatRuntime.ChatRuntime? GetRuntime(string runtimeId);
    bool RemoveRuntime(string runtimeId);
    IEnumerable<string> GetActiveRuntimeIds();
    Task<ChatMessage> SendMessageAsync(string runtimeId, ChatMessage message);


}

/// <summary>
/// Service for managing ChatRuntime instances
/// </summary>
public class ChatRuntimeService : IChatRuntimeService
{
    private readonly ConcurrentDictionary<string, ChatRuntime.ChatRuntime> _runtimes = new();
    private readonly ILogger<ChatRuntimeService> _logger;
    private readonly IStreamingEventService _streamingEvents;
    private readonly ConcurrentDictionary<string, int> _sequence = new();
    private readonly IHubContext<ChatRuntimeHub> _hubContext;

    public ChatRuntimeService(ILogger<ChatRuntimeService> logger, IStreamingEventService streamingEvents)
    {
        _logger = logger;
        _streamingEvents = streamingEvents;
    }

    private int NextSeq(string runtimeId) => _sequence.AddOrUpdate(runtimeId, 1, (_, v) => v + 1);

    /// <inheritdoc/>
    public async Task<string> CreateRuntimeAsync(string configurationType, string agentName, string instructions, bool enableStreaming)
    {
        try
        {
            var configuration = new ResearchAgentConfiguration(new TornadoApi( Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi));
            var runtime = new ChatRuntime.ChatRuntime(configuration);

            // CRITICAL FIX: Ensure the configuration gets the runtime reference 
            // This is needed so that event handlers in the configuration can access Runtime.Id
            configuration.Runtime = runtime;

            // Bridge runtime events to SignalR
            runtime.OnRuntimeEvent += async evt =>
            {
                try
                {
                    await _streamingEvents.BroadcastEventAsync(runtime.Id, new Agents.API.Models.StreamingEventResponse
                    {
                        RuntimeId = runtime.Id,
                        EventType = evt.EventType.ToString(),
                        SequenceNumber = NextSeq(runtime.Id),
                        Data = evt switch
                        {
                            ChatRuntimeOrchestrationEvent oe => new {content = oe.EventType},
                            ChatRuntimeAgentRunnerEvents se => new { content = se.AgentRunnerEvent},
                            ChatRuntimeInvokedEvent ie => new { role = ie.Message.Role?.ToString(), content = ie.Message.Content },
                            ChatRuntimeErrorEvent ee => new { error = ee.Exception.Message },
                            _ => null
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed broadcasting runtime event {Type} for {RuntimeId}", evt.EventType, runtime.Id);
                }
            };

            // CRITICAL FIX: Ensure the configuration's OnRuntimeEvent is connected to the runtime's OnRuntimeEvent
            // This creates the proper event flow: Configuration -> Runtime -> SignalR/External handlers
            configuration.OnRuntimeEvent = runtime.OnRuntimeEvent;

            _runtimes.TryAdd(runtime.Id, runtime);
            _logger.LogInformation("Created ChatRuntime with ID: {RuntimeId}", runtime.Id);
            return runtime.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ChatRuntime");
            throw;
        }
    }

    /// <inheritdoc/>
    public ChatRuntime.ChatRuntime? GetRuntime(string runtimeId)
    {
        _runtimes.TryGetValue(runtimeId, out var runtime);
        return runtime;
    }

    /// <inheritdoc/>
    public bool RemoveRuntime(string runtimeId)
    {
        if (_runtimes.TryRemove(runtimeId, out var runtime))
        {
            try
            {
                runtime.CancelExecution();
                runtime.Clear();
                _logger.LogInformation("Removed ChatRuntime with ID: {RuntimeId}", runtimeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while removing ChatRuntime {RuntimeId}", runtimeId);
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetActiveRuntimeIds() => _runtimes.Keys;

    /// <inheritdoc/>
    public async Task<ChatMessage> SendMessageAsync(string runtimeId, ChatMessage message)
    {
        var runtime = GetRuntime(runtimeId) ?? throw new ArgumentException($"Runtime with ID {runtimeId} not found");

        try
        {
            // Emit invoked event early
            runtime.OnRuntimeEvent?.Invoke(new ChatRuntimeInvokedEvent(message, runtime.Id));
            var response = await runtime.InvokeAsync(message);
            return response;
        }
        catch (Exception ex)
        {
            runtime.OnRuntimeEvent?.Invoke(new ChatRuntimeErrorEvent(ex, runtime.Id));
            _logger.LogError(ex, "Failed to send message to runtime {RuntimeId}", runtimeId);
            throw;
        }
    }
}

/// <summary>
/// Simple implementation of IRuntimeConfiguration for basic chat functionality
/// </summary>
public class SimpleChatRuntimeConfiguration : IRuntimeConfiguration
{
    private readonly List<ChatMessage> _messages = new();
    private readonly string _agentName;
    private readonly string _instructions;
    private readonly bool _enableStreaming;

    public SimpleChatRuntimeConfiguration(string agentName, string instructions, bool enableStreaming)
    {
        _agentName = agentName;
        _instructions = instructions;
        _enableStreaming = enableStreaming;
        cts = new CancellationTokenSource();
    }

    public CancellationTokenSource cts { get; set; }
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }
    public ChatRuntime.ChatRuntime Runtime { get; set; }

    public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);

        // Simulate model generation with optional streaming
        var fullResponseText = $"Echo: {message.Content}";
        var response = new ChatMessage(ChatMessageRoles.Assistant, string.Empty);
        _messages.Add(response);

        if (_enableStreaming && OnRuntimeEvent != null)
        {
            // naive chunking
            int chunk = Math.Max(4, fullResponseText.Length / 10);
            for (int i = 0; i < fullResponseText.Length; i += chunk)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var part = fullResponseText.Substring(i, Math.Min(chunk, fullResponseText.Length - i));
                response.Content += part;
                await OnRuntimeEvent(new ChatRuntimeAgentRunnerEvents(new AgentRunnerStreamingEvent(new ModelStreamingOutputTextDeltaEvent(1,1,1,part)), Runtime.Id));
                await Task.Yield();
            }
        }
        else
        {
            response.Content = fullResponseText;
        }

        return response;
    }

    public void ClearMessages() => _messages.Clear();
    public List<ChatMessage> GetMessages() => new(_messages);
    public ChatMessage GetLastMessage() => _messages.LastOrDefault() ?? new ChatMessage(ChatMessageRoles.System, "No messages");

    public void OnRuntimeInitialized()
    {
        
    }
}