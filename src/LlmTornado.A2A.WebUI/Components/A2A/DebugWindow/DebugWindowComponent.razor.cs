using LlmTornado.A2A.WebUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LlmTornado.A2A.WebUI.Components.A2A.DebugWindow;

public partial class DebugWindowComponent : ComponentBase
{
    [Parameter] public List<DebugMessage> Messages { get; set; } = new();
    [Parameter] public EventCallback<List<DebugMessage>> MessagesChanged { get; set; }

    private ElementReference debugMessagesElement;

    private async Task ClearMessages()
    {
        Messages.Clear();
        await MessagesChanged.InvokeAsync(Messages);
        StateHasChanged();
    }

    private async Task ScrollToTop()
    {
        try
        {
            await JS.InvokeAsync<object>("scrollToTop", debugMessagesElement);
        }
        catch
        {
            // Ignore JavaScript errors
        }
    }

    private string GetLevelClass(string level)
    {
        return level.ToLower() switch
        {
            "error" => "level-error",
            "warning" => "level-warning",
            "info" => "level-info",
            "debug" => "level-debug",
            _ => "level-info"
        };
    }
}