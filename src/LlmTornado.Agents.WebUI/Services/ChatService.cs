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
    /// Creates a new chat runtime
    /// </summary>
    public async Task<CreateRuntimeResponse?> CreateChatRuntimeAsync()
    {
        try
        {
            var request = new
            {
                configurationType = "simple",
                agentName = "LlmTornadoChat",
                instructions = "You are a helpful AI assistant. Be concise but informative. You can use markdown formatting in your responses.",
                enableStreaming = true
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