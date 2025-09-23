namespace LlmTornado.A2A.WebUI.Models;

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