using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo.ExampleAgents;
using LlmTornado.Demo.ExampleAgents.MagenticOneAgent;
using LlmTornado.Demo.ExampleAgents.ResearchAgent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using static LlmTornado.Demo.AgentOrchestrationRuntimeDemo;

namespace LlmTornado.Agents.API.Services;

/// <summary>
/// Interface for managing ChatRuntime instances
/// </summary>
public interface IChatRuntimeService
{
    Task<string> CreateRuntimeAsync(string configurationType);
    ChatRuntime.ChatRuntime? GetRuntime(string runtimeId);
    bool RemoveRuntime(string runtimeId);
    IEnumerable<string> GetActiveRuntimeIds();
    Task<ChatMessage> SendMessageAsync(string runtimeId, ChatMessage message);

    public string[] GetRuntimeTypes();

}

/// <summary>
/// Service for managing ChatRuntime instances
/// </summary>
public class ChatRuntimeService : IChatRuntimeService
{
    private readonly ConcurrentDictionary<string, ChatRuntime.ChatRuntime> _runtimes = new();
    private readonly ILogger<ChatRuntimeService> _logger;
    private readonly ConcurrentDictionary<string, int> _sequence = new();
    private ConcurrentDictionary<string, IRuntimeConfiguration> _configurations = new();

    public ChatRuntimeService(ILogger<ChatRuntimeService> logger)
    {
        _logger = logger;
        RegisterRuntimeConfiguration<ResearchAgentConfiguration>();
        RegisterRuntimeConfiguration<MagenticOneConfiguration>();   
    }

    public void RegisterRuntimeConfiguration<T>() where T : IRuntimeConfiguration
    {
        _configurations.AddOrUpdate(typeof(T).Name, Activator.CreateInstance<T>(), (key, oldValue) => Activator.CreateInstance<T>());
        _logger.LogInformation("Registered runtime configuration: {TypeName}", typeof(T).Name);
    }

    public string[] GetRuntimeTypes()
    {
        return _configurations.Keys.ToArray();
    }

    /// <inheritdoc/>
    public async Task<string> CreateRuntimeAsync(string configurationType)
    {
        try
        {
            IRuntimeConfiguration? configuration = Activator.CreateInstance(_configurations[configurationType].GetType()) as IRuntimeConfiguration;
            var runtime = new ChatRuntime.ChatRuntime(configuration!);

            _runtimes.TryAdd(runtime.Id, runtime);
            _logger.LogInformation("Created {configurationType} ChatRuntime with ID: {RuntimeId}", configurationType, runtime.Id);
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