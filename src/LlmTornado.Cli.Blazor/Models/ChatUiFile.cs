namespace LlmTornado.Cli.Blazor.Models;

/// <summary>
/// A file attachment on a chat message.
/// </summary>
public sealed class ChatUiFile
{
    /// <summary>
    /// Display name of the file (e.g., "screenshot.png").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., "image/png", "application/pdf").
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Raw file content bytes.
    /// </summary>
    public byte[] Content { get; set; } = [];

    /// <summary>
    /// Base64-encoded content for display or transport.
    /// Computed lazily from Content.
    /// </summary>
    public string Base64 => _base64 ??= Convert.ToBase64String(Content);
    private string? _base64;

    /// <summary>
    /// Human-readable file size.
    /// </summary>
    public string FormattedSize => Content.Length switch
    {
        < 1024 => $"{Content.Length} B",
        < 1024 * 1024 => $"{Content.Length / 1024.0:F1} KB",
        _ => $"{Content.Length / (1024.0 * 1024.0):F1} MB"
    };

    /// <summary>
    /// Whether this is an image file (for inline preview rendering).
    /// </summary>
    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether this is a PDF document.
    /// </summary>
    public bool IsDocument => MimeType == "application/pdf";

    /// <summary>
    /// Whether this is an audio file.
    /// </summary>
    public bool IsAudio => MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
}
