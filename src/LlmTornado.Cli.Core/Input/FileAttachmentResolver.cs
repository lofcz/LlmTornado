using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Images;

namespace LlmTornado.Cli.Core.Input;

/// <summary>
/// Reads files from disk and builds multipart <see cref="ChatMessage"/> instances
/// from parsed input.
/// </summary>
public static class FileAttachmentResolver
{
    /// <summary>
    /// Build a <see cref="ChatMessage"/> from parsed input.
    /// If the input contains file references, returns a multipart message with the appropriate
    /// <see cref="ChatMessagePart"/> types. Otherwise returns a plain text message.
    /// </summary>
    /// <param name="parsed">The parsed user input from <see cref="InputParser"/>.</param>
    /// <returns>A chat message ready to send, along with any errors encountered.</returns>
    public static FileAttachmentResult Resolve(ParsedInput parsed)
    {
        if (!parsed.HasFiles)
        {
            return new FileAttachmentResult
            {
                Message = new ChatMessage(ChatMessageRoles.User, parsed.CleanedText),
                Attachments = [],
                Errors = [],
            };
        }

        List<ChatMessagePart> parts = [];
        List<ResolvedAttachment> attachments = [];
        List<string> errors = [];

        // Add file parts first, then text — models typically handle this well
        foreach (ParsedFileReference fileRef in parsed.Files)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(fileRef.FilePath);
                string base64 = Convert.ToBase64String(fileBytes);

                ChatMessagePart part = fileRef.MediaType switch
                {
                    FileMediaType.Image => CreateImagePart(base64, fileRef.MimeType),
                    FileMediaType.Document => CreateDocumentPart(base64),
                    FileMediaType.Audio => CreateAudioPart(fileBytes, fileRef.MimeType),
                    _ => throw new NotSupportedException($"Unsupported media type: {fileRef.MediaType}")
                };

                parts.Add(part);
                attachments.Add(new ResolvedAttachment
                {
                    FileName = fileRef.FileName,
                    FilePath = fileRef.FilePath,
                    MediaType = fileRef.MediaType,
                    MimeType = fileRef.MimeType,
                    SizeBytes = fileBytes.Length,
                });
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to read {fileRef.FileName}: {ex.Message}");
            }
        }

        // Add text part if there's any text remaining
        if (!string.IsNullOrWhiteSpace(parsed.CleanedText))
        {
            parts.Add(new ChatMessagePart(parsed.CleanedText));
        }

        // If all files failed, fall back to plain text
        if (parts.Count == 0 || parts.All(p => p.Type == ChatMessageTypes.Text))
        {
            string text = string.IsNullOrWhiteSpace(parsed.CleanedText)
                ? "[attachment processing failed]"
                : parsed.CleanedText;

            return new FileAttachmentResult
            {
                Message = new ChatMessage(ChatMessageRoles.User, text),
                Attachments = [],
                Errors = errors,
            };
        }

        return new FileAttachmentResult
        {
            Message = new ChatMessage(ChatMessageRoles.User, parts),
            Attachments = attachments,
            Errors = errors,
        };
    }

    private static ChatMessagePart CreateImagePart(string base64, string mimeType)
    {
        // Use the data URI scheme so base64 images survive round-trip through persistence
        // (Uri.TryCreate succeeds for data: URIs)
        string dataUri = $"data:{mimeType};base64,{base64}";
        return new ChatMessagePart(dataUri, ImageDetail.Auto, mimeType);
    }

    private static ChatMessagePart CreateDocumentPart(string base64)
    {
        return new ChatMessagePart(new ChatDocument(base64));
    }

    private static ChatMessagePart CreateAudioPart(byte[] audioBytes, string mimeType)
    {
        ChatAudioFormats format = mimeType switch
        {
            "audio/wav" => ChatAudioFormats.Wav,
            "audio/mpeg" or "audio/mp3" => ChatAudioFormats.Mp3,
            // Default to wav for formats without a direct enum mapping
            _ => ChatAudioFormats.Wav,
        };

        return new ChatMessagePart(audioBytes, format);
    }
}

/// <summary>
/// Result of resolving file attachments into a chat message.
/// </summary>
public sealed class FileAttachmentResult
{
    /// <summary>
    /// The constructed chat message (multipart if attachments present, plain text otherwise).
    /// </summary>
    public required ChatMessage Message { get; init; }

    /// <summary>
    /// Successfully resolved attachments, for display/logging purposes.
    /// </summary>
    public required List<ResolvedAttachment> Attachments { get; init; }

    /// <summary>
    /// Errors encountered during file resolution.
    /// </summary>
    public required List<string> Errors { get; init; }

    /// <summary>
    /// Whether any attachments were successfully resolved.
    /// </summary>
    public bool HasAttachments => Attachments.Count > 0;
}

/// <summary>
/// Metadata about a successfully resolved file attachment.
/// </summary>
public sealed class ResolvedAttachment
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required FileMediaType MediaType { get; init; }
    public required string MimeType { get; init; }
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Format a human-readable size string.
    /// </summary>
    public string FormattedSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
