namespace LlmTornado.A2A.WebUI.Models;

/// <summary>
/// Represents a server instance in the UI
/// </summary>
public class ServerInstance
{
    public string ServerId { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public bool IsHealthy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<string> ActiveTasks { get; set; } = new();
}