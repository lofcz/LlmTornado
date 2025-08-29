using System.Text.Json;
using LlmTornado.Chat.Web.Models;
using Microsoft.JSInterop;

namespace LlmTornado.Chat.Web.Services;

/// <summary>
/// Service for handling chat operations and API communication
/// </summary>
public class ChatService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiBaseUrl;

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
    public async Task<string[]> GetRuntimeConfigurationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/chatruntime/configurations");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var configurationsResponse = JsonSerializer.Deserialize<GetConfigurationsResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return configurationsResponse?.Configurations ?? Array.Empty<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get runtime configurations: {ex.Message}");
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets list of active runtime instances
    /// </summary>
    public async Task<RuntimeInfo[]> GetActiveRuntimesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/chatruntime/list");
            if (response.IsSuccessStatusCode)
            {
                var runtimeIds = await response.Content.ReadFromJsonAsync<string[]>();
                var runtimeInfos = new List<RuntimeInfo>();
                
                if (runtimeIds != null)
                {
                    foreach (var runtimeId in runtimeIds)
                    {
                        var statusResponse = await _httpClient.GetAsync($"api/chatruntime/{runtimeId}/status");
                        if (statusResponse.IsSuccessStatusCode)
                        {
                            var status = await statusResponse.Content.ReadFromJsonAsync<RuntimeStatusResponse>();
                            if (status != null)
                            {
                                runtimeInfos.Add(new RuntimeInfo
                                {
                                    RuntimeId = runtimeId,
                                    Status = status.Status,
                                    MessageCount = status.MessageCount,
                                    DisplayName = $"{runtimeId[..8]}... ({status.MessageCount} msgs)"
                                });
                            }
                        }
                    }
                }
                
                return runtimeInfos.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get active runtimes: {ex.Message}");
        }
        return Array.Empty<RuntimeInfo>();
    }

    /// <summary>
    /// Creates a new chat runtime with specified configuration type
    /// </summary>
    public async Task<CreateRuntimeResponse?> CreateChatRuntimeAsync(string configurationType = "simple")
    {
        try
        {
            var request = new
            {
                configurationType = configurationType
            };

            var response = await _httpClient.PostAsJsonAsync("api/chatruntime/create", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CreateRuntimeResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create chat runtime: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Removes/deletes a specific runtime
    /// </summary>
    public async Task<bool> RemoveRuntimeAsync(string runtimeId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/chatruntime/{runtimeId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to remove runtime: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets status information for a specific runtime
    /// </summary>
    public async Task<RuntimeStatusResponse?> GetRuntimeStatusAsync(string runtimeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/chatruntime/{runtimeId}/status");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RuntimeStatusResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get runtime status: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Starts streaming a message using EventSource
    /// </summary>
    public async Task StreamMessageAsync<T>(string runtimeId, string message, DotNetObjectReference<T> componentRef) where T : class
    {
        try
        {
            var request = new { content = message };
            await _jsRuntime.InvokeVoidAsync("startEventSource", 
                $"{_apiBaseUrl}/api/chatruntime/{runtimeId}/stream",
                JsonSerializer.Serialize(request),
                componentRef);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stream message: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads theme preference from localStorage
    /// </summary>
    public async Task<bool> LoadThemePreferenceAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("getTheme");
            return savedTheme == "dark";
        }
        catch
        {
            return true; // Default to dark mode
        }
    }

    /// <summary>
    /// Saves theme preference to localStorage
    /// </summary>
    public async Task SaveThemePreferenceAsync(bool isDarkMode)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("setTheme", isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    /// <summary>
    /// Scrolls chat messages to bottom
    /// </summary>
    public async Task ScrollToBottomAsync()
    {
        try
        {
            await Task.Delay(100);
            await _jsRuntime.InvokeVoidAsync("smoothScrollTo", "chat-messages", "bottom");
        }
        catch
        {
            // Ignore scrolling errors
        }
    }

    /// <summary>
    /// Scrolls log content to bottom
    /// </summary>
    public async Task ScrollLogToBottomAsync()
    {
        try
        {
            await Task.Delay(100);
            await _jsRuntime.InvokeVoidAsync("smoothScrollTo", "log-content", "bottom");
        }
        catch
        {
            // Ignore scrolling errors
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}