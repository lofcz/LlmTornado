using LlmTornado.A2A.WebUI.Models;
using LlmTornado.A2A.Hosting.Models;
using Microsoft.AspNetCore.Components;

namespace LlmTornado.A2A.WebUI.Components.A2A.ServerManagement;

public partial class ServerManagementComponent : ComponentBase
{
    [Parameter] public ServerInstance? SelectedServer { get; set; }
    [Parameter] public EventCallback<ServerInstance?> SelectedServerChanged { get; set; }
    [Parameter] public string ApiKey { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ApiKeyChanged { get; set; }
    [Parameter] public List<ServerInstance> Servers { get; set; } = new();
    [Parameter] public EventCallback<ServerInstance> OnServerCreated { get; set; }
    [Parameter] public EventCallback<string> OnServerDeleted { get; set; }

    private List<string> availableConfigurations = new();
    private string selectedConfiguration = string.Empty;
    private bool isCreating = false;
    private bool isDeleting = false;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigurations();
    }

    private async Task LoadConfigurations()
    {
        try
        {
            availableConfigurations = (await ApiService.GetServerConfigurationsAsync()).ToList();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load configurations: {ex.Message}";
        }
    }

    private async Task CreateServer()
    {
        if (string.IsNullOrEmpty(selectedConfiguration) || string.IsNullOrEmpty(ApiKey))
            return;

        isCreating = true;
        errorMessage = string.Empty;

        try
        {
            var request = new ServerCreationRequest
            {
                AgentImageKey = selectedConfiguration,
                EnvironmentVariables = new[] { $"{ApiKey}" }
            };

            var result = await ApiService.CreateServerAsync(request);

            if (result.Success && !string.IsNullOrEmpty(result.ServerId))
            {
                var server = new ServerInstance
                {
                    ServerId = result.ServerId,
                    Configuration = selectedConfiguration,
                    Endpoint = result.Endpoint ?? string.Empty,
                    Status = "Running",
                    IsHealthy = true,
                    CreatedAt = DateTime.Now
                };

                await OnServerCreated.InvokeAsync(server);
            }
            else
            {
                errorMessage = result.ErrorMessage ?? "Failed to create server";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error creating server: {ex.Message}";
        }
        finally
        {
            isCreating = false;
            StateHasChanged();
        }
    }

    private async Task DeleteSelectedServer()
    {
        if (SelectedServer == null) return;

        isDeleting = true;
        errorMessage = string.Empty;

        try
        {
            await ApiService.DeleteServerAsync(SelectedServer.ServerId);
            await OnServerDeleted.InvokeAsync(SelectedServer.ServerId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting server: {ex.Message}";
        }
        finally
        {
            isDeleting = false;
            StateHasChanged();
        }
    }

    private async Task SelectServer(ServerInstance server)
    {
        SelectedServer = server;
        await SelectedServerChanged.InvokeAsync(server);
    }

    private string GetStatusClass(string status)
    {
        return status.ToLower() switch
        {
            "running" => "status-running",
            "stopped" => "status-stopped",
            "error" => "status-error",
            _ => "status-unknown"
        };
    }

    private void ClearError()
    {
        errorMessage = string.Empty;
    }
}