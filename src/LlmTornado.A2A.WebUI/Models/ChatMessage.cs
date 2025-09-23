namespace LlmTornado.A2A.WebUI.Models;

/// <summary>
/// Represents a chat message in the UI
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? TaskId { get; set; }
    public bool IsFromUser { get; set; }
    public List<FileAttachment> Attachments { get; set; } = new();
}