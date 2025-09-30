using LlmTornado.A2A.WebUI.Models;
using Microsoft.AspNetCore.Components;
using System.Timers;

namespace LlmTornado.A2A.WebUI.Components.A2A.ServerMonitor;

public partial class ServerMonitorComponent : ComponentBase, IDisposable
{
    [Parameter] public List<ServerInstance> Servers { get; set; } = new();
    [Parameter] public ServerInstance? SelectedServer { get; set; }
    [Parameter] public EventCallback<ServerInstance?> SelectedServerChanged { get; set; }
    [Parameter] public EventCallback<ServerInstance> OnServerStatusChanged { get; set; }

    private System.Timers.Timer? refreshTimer;
    private bool isRefreshing = false;
    private DateTime? lastRefreshTime;

    protected override void OnInitialized()
    {
        // Auto-refresh every 30 seconds
        refreshTimer = new System.Timers.Timer(30000);
        refreshTimer.Elapsed += async (sender, e) => await RefreshAllStatuses();
        refreshTimer.Start();
    }

    private async Task SelectServer(ServerInstance server)
    {
        SelectedServer = server;
        await SelectedServerChanged.InvokeAsync(server);
    }

    private async Task RefreshAllStatuses()
    {
        if (isRefreshing) return;

        isRefreshing = true;
        lastRefreshTime = DateTime.Now;

        try
        {
            var tasks = Servers.Select(RefreshServerStatus);
            await Task.WhenAll(tasks);
        }
        finally
        {
            isRefreshing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RefreshServerStatus(ServerInstance server)
    {
        try
        {
            var status = await ApiService.GetServerStatusAsync(server.ServerId);
            var oldStatus = server.Status;
            var oldHealth = server.IsHealthy;

            server.Status = status.Status;
            server.IsHealthy = status.IsHealthy;
            server.Endpoint = status.Endpoint ?? server.Endpoint;

            // Notify if status changed
            if (oldStatus != server.Status || oldHealth != server.IsHealthy)
            {
                await OnServerStatusChanged.InvokeAsync(server);
            }
        }
        catch (Exception)
        {
            // Mark as unhealthy if we can't get status
            server.IsHealthy = false;
            server.Status = "Error";
        }
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

    private string GetStatusIcon(string status)
    {
        return status.ToLower() switch
        {
            "running" => "🟢",
            "stopped" => "🔴",
            "error" => "❌",
            _ => "⚪"
        };
    }

    private string GetUptime(DateTime createdAt)
    {
        var uptime = DateTime.Now - createdAt;
        if (uptime.TotalDays >= 1)
            return $"{uptime.Days}d {uptime.Hours}h";
        if (uptime.TotalHours >= 1)
            return $"{uptime.Hours}h {uptime.Minutes}m";
        return $"{uptime.Minutes}m";
    }

    public void Dispose()
    {
        refreshTimer?.Stop();
        refreshTimer?.Dispose();
    }
}