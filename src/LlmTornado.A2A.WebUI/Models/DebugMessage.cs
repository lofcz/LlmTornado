namespace LlmTornado.A2A.WebUI.Models;

/// <summary>
/// Represents debug/log information
/// </summary>
public class DebugMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Level { get; set; } = "Info"; // Info, Warning, Error
}
