namespace LlmTornado.Chat.Web.Models;

/// <summary>
/// Represents a chat message in the UI
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = "";
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string CssClass { get; set; } = "";
    public bool IsHtml { get; set; } = false;
    public bool IsMarkdown { get; set; } = false;
    public string? Base64File { get; set; } = "";
}

/// <summary>
/// Represents a log event in the debug panel
/// </summary>
public class LogEvent
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Details { get; set; } = "";
    public string Data { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string CssClass { get; set; } = "";
}

/// <summary>
/// Response from chat runtime creation API
/// </summary>
public class CreateRuntimeResponse
{
    public string? RuntimeId { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Event data for text delta streaming
/// </summary>
public class TextDeltaEvent
{
    public string? Text { get; set; }
}

/// <summary>
/// Event data for tool invocation
/// </summary>
public class ToolEvent
{
    public string? ToolName { get; set; }
    public string? Parameters { get; set; }
}

/// <summary>
/// Event data for reasoning updates
/// </summary>
public class ReasoningEvent
{
    public string? Text { get; set; }
}