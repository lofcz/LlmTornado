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
/// Response from runtime configurations API
/// </summary>
public class GetConfigurationsResponse
{
    public string[] Configurations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Runtime status information
/// </summary>
public class RuntimeStatusResponse
{
    public string RuntimeId { get; set; } = "";
    public string Status { get; set; } = "";
    public bool StreamingEnabled { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// Information about an active runtime
/// </summary>
public class RuntimeInfo
{
    public string RuntimeId { get; set; } = "";
    public string Status { get; set; } = "";
    public int MessageCount { get; set; }
    public string DisplayName { get; set; } = "";
    public string ConfigurationType { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
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


/// <summary>
/// Information about an uploaded file
/// </summary>
public class FileInfo
{
    public bool IsValid { get; set; }
    public string MimeType { get; set; } = "";
    public long SizeInBytes { get; set; }
    public string Base64Data { get; set; } = "";

    public string FormattedSize
    {
        get
        {
            if (SizeInBytes < 1024) return $"{SizeInBytes} B";
            if (SizeInBytes < 1024 * 1024) return $"{SizeInBytes / 1024:F1} KB";
            if (SizeInBytes < 1024 * 1024 * 1024) return $"{SizeInBytes / (1024 * 1024):F1} MB";
            return $"{SizeInBytes / (1024 * 1024 * 1024):F1} GB";
        }
    }

    public bool IsImage => MimeType.StartsWith("image/");
    public bool IsPdf => MimeType == "application/pdf";
    public bool IsAudio => MimeType.StartsWith("audio/");
    public bool IsVideo => MimeType.StartsWith("video/");

    /// <summary>
    /// Gets the MIME type and size information for a file
    /// </summary>
    /// <param name="base64Data">Base64 data string with MIME type prefix</param>
    /// <returns>File information object</returns>
    public FileInfo GetFileInfo(string base64Data)
    {
        if (string.IsNullOrEmpty(base64Data) || !base64Data.StartsWith("data:"))
        {
            return new FileInfo { IsValid = false };
        }

        try
        {
            var parts = base64Data.Split(';');
            if (parts.Length < 2) return new FileInfo { IsValid = false };

            var mimeType = parts[0].Substring(5); // Remove "data:" prefix
            var base64Part = parts[1];

            if (!base64Part.StartsWith("base64,"))
            {
                return new FileInfo { IsValid = false };
            }

            var base64String = base64Part.Substring(7); // Remove "base64," prefix
            var sizeInBytes = (base64String.Length * 3) / 4; // Approximate size

            return new FileInfo
            {
                IsValid = true,
                MimeType = mimeType,
                SizeInBytes = sizeInBytes,
                Base64Data = base64String
            };
        }
        catch
        {
            return new FileInfo { IsValid = false };
        }
    }
}