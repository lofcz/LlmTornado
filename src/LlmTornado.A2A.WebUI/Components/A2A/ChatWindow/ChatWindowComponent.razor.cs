using LlmTornado.A2A.WebUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace LlmTornado.A2A.WebUI.Components.A2A.ChatWindow;

public partial class ChatWindowComponent : ComponentBase
{
    [Parameter] public List<ChatMessage> Messages { get; set; } = new();
    [Parameter] public EventCallback<List<ChatMessage>> MessagesChanged { get; set; }

    private ElementReference chatMessagesElement;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Messages.Any())
        {
            await ScrollToBottom();
        }
    }

    private async Task ClearMessages()
    {
        Messages.Clear();
        await MessagesChanged.InvokeAsync(Messages);
        StateHasChanged();
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await JS.InvokeAsync<object>("scrollToBottom", chatMessagesElement);
        }
        catch
        {
            // Ignore JavaScript errors
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
        return $"{bytes / (1024 * 1024 * 1024):F1} GB";
    }

    private string FormatMessageContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Convert newlines to HTML breaks
        var formatted = content.Replace("\n", "<br/>");
        
        // Simple URL detection and linking
        var urlPattern = @"(https?://[^\s<>""]+)";
        formatted = Regex.Replace(formatted, urlPattern, "<a href=\"$1\" target=\"_blank\">$1</a>");
        
        return formatted;
    }
}