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

/// <summary>
/// Represents a file attachment
/// </summary>
public class FileAttachment
{
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Base64Data { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsImage => MimeType.StartsWith("image/");
}

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