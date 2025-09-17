using Microsoft.JSInterop;

namespace LlmTornado.Chat.Web.Services;

public partial class ChatService
{
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
}
