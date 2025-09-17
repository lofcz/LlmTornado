using System.Text.Json;
using LlmTornado.Chat.Web.Models;
using Microsoft.JSInterop;

namespace LlmTornado.Chat.Web.Services;


public interface IChatService
{
    Task<string[]> GetRuntimeConfigurationsAsync();
    Task<RuntimeInfo[]> GetActiveRuntimesAsync();
    Task<CreateRuntimeResponse?> CreateChatRuntimeAsync(string configurationType = "simple");
    Task<bool> RemoveRuntimeAsync(string runtimeId);
    Task<RuntimeStatusResponse?> GetRuntimeStatusAsync(string runtimeId);
    Task StreamMessageAsync<T>(string runtimeId, string message, string? base64, DotNetObjectReference<T> componentRef) where T : class;
}

/// <summary>
/// Service for handling chat operations and API communication
/// </summary>
public partial class ChatService : IChatService, IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly IJSRuntime _jsRuntime;
    protected readonly string _apiBaseUrl;

    public ChatService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _apiBaseUrl = configuration["ChatApi:BaseUrl"] ?? "https://localhost:7242";
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
    }

    /// <summary>
    /// Gets available runtime configuration types
    /// </summary>
    public virtual async Task<string[]> GetRuntimeConfigurationsAsync()
    {
       throw new NotImplementedException();
    }

    /// <summary>
    /// Gets list of active runtime instances
    /// </summary>
    public virtual async Task<RuntimeInfo[]> GetActiveRuntimesAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a new chat runtime with specified configuration type
    /// </summary>
    public virtual async Task<CreateRuntimeResponse?> CreateChatRuntimeAsync(string configurationType = "simple")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes/deletes a specific runtime
    /// </summary>
    public virtual async Task<bool> RemoveRuntimeAsync(string runtimeId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets status information for a specific runtime
    /// </summary>
    public virtual async Task<RuntimeStatusResponse?> GetRuntimeStatusAsync(string runtimeId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts streaming a message using EventSource
    /// </summary>
    public virtual async Task StreamMessageAsync<T>(string runtimeId, string message, string? base64, DotNetObjectReference<T> componentRef) where T : class
    {
        throw new NotImplementedException();
    }


    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}