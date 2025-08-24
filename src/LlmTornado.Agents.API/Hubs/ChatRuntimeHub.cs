using Microsoft.AspNetCore.SignalR;
using LlmTornado.Agents.API.Models;
using LlmTornado.Agents.API.Services;
using LlmTornado.Agents.DataModels;

namespace LlmTornado.Agents.API.Hubs;

/// <summary>
/// SignalR hub for streaming ChatRuntime events
/// </summary>
public class ChatRuntimeHub : Hub
{
    private readonly IChatRuntimeService _runtimeService;
    private readonly ILogger<ChatRuntimeHub> _logger;

    public ChatRuntimeHub(IChatRuntimeService runtimeService, ILogger<ChatRuntimeHub> logger)
    {
        _runtimeService = runtimeService;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to events from a specific runtime
    /// </summary>
    /// <param name="runtimeId">The runtime ID to subscribe to</param>
    public async Task SubscribeToRuntime(string runtimeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"runtime-{runtimeId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to runtime {RuntimeId}", 
            Context.ConnectionId, runtimeId);
    }

    /// <summary>
    /// Unsubscribe from events from a specific runtime
    /// </summary>
    /// <param name="runtimeId">The runtime ID to unsubscribe from</param>
    public async Task UnsubscribeFromRuntime(string runtimeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"runtime-{runtimeId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from runtime {RuntimeId}", 
            Context.ConnectionId, runtimeId);
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Service for broadcasting streaming events to SignalR clients
/// </summary>
public interface IStreamingEventService
{
    /// <summary>
    /// Broadcast a streaming event to all subscribers of a runtime
    /// </summary>
    Task BroadcastEventAsync(string runtimeId, StreamingEventResponse eventResponse);
}

/// <summary>
/// Implementation of streaming event service using SignalR
/// </summary>
public class SignalRStreamingEventService : IStreamingEventService
{
    private readonly IHubContext<ChatRuntimeHub> _hubContext;
    private readonly ILogger<SignalRStreamingEventService> _logger;

    public SignalRStreamingEventService(IHubContext<ChatRuntimeHub> hubContext, ILogger<SignalRStreamingEventService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task BroadcastEventAsync(string runtimeId, StreamingEventResponse eventResponse)
    {
        try
        {
            await _hubContext.Clients.Group($"runtime-{runtimeId}")
                .SendAsync("ReceiveStreamingEvent", eventResponse);
            
            _logger.LogDebug("Broadcasted event {EventType} to runtime {RuntimeId} subscribers", 
                eventResponse.EventType, runtimeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast event to runtime {RuntimeId}", runtimeId);
        }
    }
}