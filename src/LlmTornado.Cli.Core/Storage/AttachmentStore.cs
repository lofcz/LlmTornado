namespace LlmTornado.Cli.Core.Storage;

/// <summary>
/// Media type categories for attachments.
/// </summary>
public enum AttachmentMediaType
{
    Image = 0,
    Document = 1,
    Audio = 2,
}

/// <summary>
/// Metadata about a stored attachment (corresponds to the attachments DB table).
/// </summary>
public sealed class AttachmentMetadata
{
    public required string Id { get; init; }
    public required string MessageId { get; init; }
    public required string ConversationId { get; init; }
    public string? FileName { get; init; }
    public required string MimeType { get; init; }
    public required AttachmentMediaType MediaType { get; init; }
    public long SizeBytes { get; init; }
    public required string StoragePath { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Manages binary attachment files on disk alongside the SQLite database.
/// Files are stored as raw bytes under {attachmentsDir}/{conversationId}/{attachmentId}.{ext}.
/// </summary>
public sealed class AttachmentStore
{
    private readonly string _rootDirectory;

    public AttachmentStore(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(_rootDirectory);
    }

    /// <summary>
    /// Save binary data to a file and return the relative storage path.
    /// </summary>
    public string SaveAttachment(string conversationId, string attachmentId, byte[] data, string extension)
    {
        string convDir = Path.Combine(_rootDirectory, conversationId);
        Directory.CreateDirectory(convDir);

        string fileName = $"{attachmentId}{extension}";
        string fullPath = Path.Combine(convDir, fileName);

        File.WriteAllBytes(fullPath, data);

        // Return relative path from root
        return Path.Combine(conversationId, fileName);
    }

    /// <summary>
    /// Load binary data for an attachment by its relative storage path.
    /// Returns null if the file does not exist.
    /// </summary>
    public byte[]? LoadAttachment(string storagePath)
    {
        string fullPath = Path.Combine(_rootDirectory, storagePath);
        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    /// <summary>
    /// Delete all attachment files for a conversation.
    /// </summary>
    public void DeleteConversationAttachments(string conversationId)
    {
        string convDir = Path.Combine(_rootDirectory, conversationId);
        if (Directory.Exists(convDir))
        {
            try { Directory.Delete(convDir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Get file extension for a MIME type.
    /// </summary>
    public static string GetExtensionForMime(string? mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            "image/tiff" => ".tiff",
            "application/pdf" => ".pdf",
            "audio/wav" => ".wav",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            "audio/mp4" or "audio/m4a" => ".m4a",
            "audio/aac" => ".aac",
            "audio/webm" => ".webm",
            _ => ".bin"
        };
    }
}
