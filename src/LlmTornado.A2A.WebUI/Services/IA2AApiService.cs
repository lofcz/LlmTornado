using LlmTornado.A2A.Hosting.Models;
using A2A;

namespace LlmTornado.A2A.WebUI.Services;

/// <summary>
/// Interface for A2A API operations
/// </summary>
public interface IA2AApiService
{
    /// <summary>
    /// Get available server configurations
    /// </summary>
    Task<string[]> GetServerConfigurationsAsync();
    
    /// <summary>
    /// Get list of active servers
    /// </summary>
    Task<ServerInfo[]> GetActiveServersAsync();
    
    /// <summary>
    /// Create a new A2A server
    /// </summary>
    Task<ServerCreationResult> CreateServerAsync(ServerCreationRequest request);
    
    /// <summary>
    /// Delete an A2A server
    /// </summary>
    Task DeleteServerAsync(string serverId);
    
    /// <summary>
    /// Get server status
    /// </summary>
    Task<ServerStatus> GetServerStatusAsync(string serverId);
    
    /// <summary>
    /// Get agent card information
    /// </summary>
    Task<AgentCard> GetAgentCardAsync(string endpoint);
    
    /// <summary>
    /// Send message to server
    /// </summary>
    Task<AgentMessage> SendMessageAsync(string endpoint, List<Part> parts);
    
    /// <summary>
    /// Get task status
    /// </summary>
    Task<AgentTask> GetTaskStatusAsync(string endpoint, string taskId);
    
    /// <summary>
    /// Cancel a task
    /// </summary>
    Task CancelTaskAsync(string endpoint, string taskId);
}