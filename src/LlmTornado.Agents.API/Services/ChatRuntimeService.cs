using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Collections.Concurrent;

namespace LlmTornado.Agents.API.Services;

/// <summary>
/// Interface for managing ChatRuntime instances
/// </summary>
public interface IChatRuntimeService
{
    /// <summary>
    /// Creates a new ChatRuntime instance
    /// </summary>
    Task<string> CreateRuntimeAsync(string configurationType, string agentName, string instructions, bool enableStreaming);
    
    /// <summary>
    /// Gets a ChatRuntime instance by ID
    /// </summary>
    ChatRuntime.ChatRuntime? GetRuntime(string runtimeId);
    
    /// <summary>
    /// Removes a ChatRuntime instance
    /// </summary>
    bool RemoveRuntime(string runtimeId);
    
    /// <summary>
    /// Gets all active runtime IDs
    /// </summary>
    IEnumerable<string> GetActiveRuntimeIds();
    
    /// <summary>
    /// Sends a message to a specific runtime
    /// </summary>
    Task<ChatMessage> SendMessageAsync(string runtimeId, ChatMessage message);
}

/// <summary>
/// Service for managing ChatRuntime instances
/// </summary>
public class ChatRuntimeService : IChatRuntimeService
{
    private readonly ConcurrentDictionary<string, ChatRuntime.ChatRuntime> _runtimes = new();
    private readonly ILogger<ChatRuntimeService> _logger;

    public ChatRuntimeService(ILogger<ChatRuntimeService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> CreateRuntimeAsync(string configurationType, string agentName, string instructions, bool enableStreaming)
    {
        try
        {
            // Create a basic configuration for now
            // In a real implementation, you'd have different configuration types
            var configuration = new SimpleChatRuntimeConfiguration(agentName, instructions, enableStreaming);
            
            var runtime = new ChatRuntime.ChatRuntime(configuration);
            
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
    public IEnumerable<string> GetActiveRuntimeIds()
    {
        return _runtimes.Keys;
    }

    /// <inheritdoc/>
    public async Task<ChatMessage> SendMessageAsync(string runtimeId, ChatMessage message)
    {
        var runtime = GetRuntime(runtimeId);
        if (runtime == null)
        {
            throw new ArgumentException($"Runtime with ID {runtimeId} not found");
        }

        try
        {
            var response = await runtime.RuntimeConfiguration.AddToChatAsync(message);
            return response;
        }
        catch (Exception ex)
        {
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

    public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        
        // For now, just echo back a simple response
        // In a real implementation, this would integrate with the actual agent
        var response = new ChatMessage(ChatMessageRoles.Assistant, $"Echo: {message.Content}");
        _messages.Add(response);
        
        return response;
    }

    public void ClearMessages()
    {
        _messages.Clear();
    }

    public List<ChatMessage> GetMessages()
    {
        return new List<ChatMessage>(_messages);
    }

    public ChatMessage GetLastMessage()
    {
        return _messages.LastOrDefault() ?? new ChatMessage(ChatMessageRoles.System, "No messages");
    }
}