using System.Text.Json;
using System.Text.Json.Serialization;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Images;

namespace LlmTornado.Cli.Core.Storage;

/// <summary>
/// A lightweight representation of a <see cref="ChatMessagePart"/> for JSON storage.
/// Binary data is replaced with attachment references; text/reasoning parts are inline.
/// </summary>
internal sealed class SerializedPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// For images: either a URL or <c>attachment:{id}</c> reference.
    /// </summary>
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_mime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageMimeType { get; set; }

    [JsonPropertyName("image_detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageDetail { get; set; }

    /// <summary>
    /// For audio: <c>attachment:{id}</c> reference or inline data for small clips.
    /// </summary>
    [JsonPropertyName("audio_ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AudioRef { get; set; }

    [JsonPropertyName("audio_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AudioFormat { get; set; }

    [JsonPropertyName("audio_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AudioUrl { get; set; }

    /// <summary>
    /// For documents: <c>attachment:{id}</c> reference.
    /// </summary>
    [JsonPropertyName("doc_ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentRef { get; set; }

    [JsonPropertyName("doc_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Reasoning/thinking content (text, no binary).
    /// </summary>
    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; set; }

    [JsonPropertyName("reasoning_sig")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningSignature { get; set; }
}

/// <summary>
/// Info about an attachment extracted during serialization.
/// </summary>
public sealed class ExtractedAttachment
{
    public required string Id { get; init; }
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public required AttachmentMediaType MediaType { get; init; }
    public string? FileName { get; init; }
    public required string Extension { get; init; }
}

/// <summary>
/// Result of serializing a ChatMessage for database storage.
/// </summary>
public sealed class SerializedMessage
{
    public required string Role { get; init; }
    public string? Content { get; init; }
    public string? PartsJson { get; init; }
    public int TokenEstimate { get; init; }
    public List<ExtractedAttachment> Attachments { get; init; } = [];
}

/// <summary>
/// Handles conversion between <see cref="ChatMessage"/> and database row representation.
/// Binary attachment data is extracted during serialization and replaced with
/// <c>attachment:{id}</c> references in the stored JSON. On load, these references
/// can be resolved back to full ChatMessagePart instances via the AttachmentStore.
/// </summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serialize a ChatMessage into a database-ready representation.
    /// Binary data is extracted into <see cref="ExtractedAttachment"/> instances.
    /// </summary>
    public static SerializedMessage Serialize(ChatMessage message)
    {
        string role = message.Role?.ToString()?.ToLowerInvariant() ?? "user";
        int tokenEstimate = Memory.CompressionStrategy.EstimateTokens(message);

        if (message.Parts is not { Count: > 0 })
        {
            return new SerializedMessage
            {
                Role = role,
                Content = message.Content,
                PartsJson = null,
                TokenEstimate = tokenEstimate,
            };
        }

        List<SerializedPart> serializedParts = [];
        List<ExtractedAttachment> attachments = [];

        foreach (ChatMessagePart part in message.Parts)
        {
            SerializedPart sp = new() { Type = part.Type.ToString() };

            switch (part.Type)
            {
                case ChatMessageTypes.Text:
                    sp.Text = part.Text;
                    break;

                case ChatMessageTypes.Image when part.Image is not null:
                {
                    string? imageData = part.Image.Url;
                    if (!string.IsNullOrEmpty(imageData) && IsBase64Content(imageData))
                    {
                        // Extract binary data to attachment
                        string attId = Guid.NewGuid().ToString("N");
                        byte[] bytes = ExtractBase64Bytes(imageData);
                        string mime = part.Image.MimeType ?? GuessMimeFromDataUri(imageData) ?? "image/png";
                        string ext = AttachmentStore.GetExtensionForMime(mime);

                        attachments.Add(new ExtractedAttachment
                        {
                            Id = attId,
                            Data = bytes,
                            MimeType = mime,
                            MediaType = AttachmentMediaType.Image,
                            Extension = ext,
                        });

                        sp.ImageUrl = $"attachment:{attId}";
                        sp.ImageMimeType = mime;
                    }
                    else
                    {
                        // URL reference — store as-is
                        sp.ImageUrl = imageData;
                        sp.ImageMimeType = part.Image.MimeType;
                    }

                    sp.ImageDetail = part.Image.Detail?.ToString();
                    break;
                }

                case ChatMessageTypes.Audio when part.Audio is not null:
                {
                    if (!string.IsNullOrEmpty(part.Audio.Data))
                    {
                        // Inline base64 audio → extract
                        string attId = Guid.NewGuid().ToString("N");
                        byte[] bytes = Convert.FromBase64String(part.Audio.Data);
                        string mime = GetMimeForAudioFormat(part.Audio.Format);
                        string ext = AttachmentStore.GetExtensionForMime(mime);

                        attachments.Add(new ExtractedAttachment
                        {
                            Id = attId,
                            Data = bytes,
                            MimeType = mime,
                            MediaType = AttachmentMediaType.Audio,
                            Extension = ext,
                        });

                        sp.AudioRef = $"attachment:{attId}";
                    }
                    else if (part.Audio.Url is not null)
                    {
                        sp.AudioUrl = part.Audio.Url.AbsoluteUri;
                    }

                    sp.AudioFormat = part.Audio.Format?.ToString();
                    break;
                }

                case ChatMessageTypes.Document when part.Document is not null:
                {
                    if (!string.IsNullOrEmpty(part.Document.Base64))
                    {
                        // Base64 PDF → extract
                        string attId = Guid.NewGuid().ToString("N");
                        byte[] bytes = Convert.FromBase64String(part.Document.Base64);

                        attachments.Add(new ExtractedAttachment
                        {
                            Id = attId,
                            Data = bytes,
                            MimeType = "application/pdf",
                            MediaType = AttachmentMediaType.Document,
                            Extension = ".pdf",
                        });

                        sp.DocumentRef = $"attachment:{attId}";
                    }
                    else if (part.Document.Uri is not null)
                    {
                        sp.DocumentUrl = part.Document.Uri.AbsoluteUri;
                    }
                    break;
                }

                case ChatMessageTypes.Reasoning when part.Reasoning is not null:
                {
                    sp.ReasoningContent = part.Reasoning.Content;
                    sp.ReasoningSignature = part.Reasoning.Signature;
                    break;
                }

                default:
                    // For other types (SearchResult, FileLink, etc.), store text if available
                    sp.Text = part.Text;
                    break;
            }

            serializedParts.Add(sp);
        }

        string partsJson = JsonSerializer.Serialize(serializedParts, JsonOpts);

        return new SerializedMessage
        {
            Role = role,
            Content = message.Content,
            PartsJson = partsJson,
            TokenEstimate = tokenEstimate,
            Attachments = attachments,
        };
    }

    /// <summary>
    /// Deserialize a database row back to a ChatMessage WITHOUT resolving attachment references.
    /// Binary parts will have <c>attachment:{id}</c> placeholder URLs — suitable for
    /// lightweight UI loads where images are lazy-loaded on demand.
    /// </summary>
    public static ChatMessage DeserializeLightweight(string role, string? content, string? partsJson, Guid messageId)
    {
        ChatMessageRoles chatRole = ParseRole(role);

        if (string.IsNullOrEmpty(partsJson))
        {
            return new ChatMessage(chatRole, content ?? "", messageId);
        }

        List<SerializedPart>? parts = JsonSerializer.Deserialize<List<SerializedPart>>(partsJson, JsonOpts);
        if (parts is null or { Count: 0 })
        {
            return new ChatMessage(chatRole, content ?? "", messageId);
        }

        List<ChatMessagePart> chatParts = [];
        foreach (SerializedPart sp in parts)
        {
            ChatMessagePart? chatPart = DeserializePartLightweight(sp);
            if (chatPart is not null)
                chatParts.Add(chatPart);
        }

        return chatParts.Count > 0
            ? new ChatMessage(chatRole, chatParts, messageId)
            : new ChatMessage(chatRole, content ?? "", messageId);
    }

    /// <summary>
    /// Deserialize a database row and resolve all attachment references to full inline data.
    /// Used when building the message list for LLM context.
    /// </summary>
    public static ChatMessage DeserializeWithAttachments(
        string role, string? content, string? partsJson, Guid messageId,
        AttachmentStore attachmentStore,
        Dictionary<string, AttachmentMetadata> attachmentMap)
    {
        ChatMessageRoles chatRole = ParseRole(role);

        if (string.IsNullOrEmpty(partsJson))
        {
            return new ChatMessage(chatRole, content ?? "", messageId);
        }

        List<SerializedPart>? parts = JsonSerializer.Deserialize<List<SerializedPart>>(partsJson, JsonOpts);
        if (parts is null or { Count: 0 })
        {
            return new ChatMessage(chatRole, content ?? "", messageId);
        }

        List<ChatMessagePart> chatParts = [];
        foreach (SerializedPart sp in parts)
        {
            ChatMessagePart? chatPart = DeserializePartFull(sp, attachmentStore, attachmentMap);
            if (chatPart is not null)
                chatParts.Add(chatPart);
        }

        return chatParts.Count > 0
            ? new ChatMessage(chatRole, chatParts, messageId)
            : new ChatMessage(chatRole, content ?? "", messageId);
    }

    /// <summary>
    /// Resolve a single attachment reference to raw bytes + mime type.
    /// Used for lazy-loading a single image in the UI.
    /// </summary>
    public static (byte[] data, string mimeType)? ResolveAttachment(
        AttachmentStore store, AttachmentMetadata metadata)
    {
        byte[]? data = store.LoadAttachment(metadata.StoragePath);
        return data is not null ? (data, metadata.MimeType) : null;
    }

    // ───────────────────────────────────────────────
    // Private helpers
    // ───────────────────────────────────────────────

    private static ChatMessagePart? DeserializePartLightweight(SerializedPart sp)
    {
        return sp.Type switch
        {
            nameof(ChatMessageTypes.Text) =>
                !string.IsNullOrEmpty(sp.Text) ? new ChatMessagePart(sp.Text) : null,

            nameof(ChatMessageTypes.Image) =>
                DeserializeImagePartLightweight(sp),

            nameof(ChatMessageTypes.Audio) =>
                DeserializeAudioPartLightweight(sp),

            nameof(ChatMessageTypes.Document) =>
                DeserializeDocumentPartLightweight(sp),

            nameof(ChatMessageTypes.Reasoning) =>
                DeserializeReasoningPart(sp),

            _ => !string.IsNullOrEmpty(sp.Text) ? new ChatMessagePart(sp.Text) : null,
        };
    }

    private static ChatMessagePart? DeserializePartFull(
        SerializedPart sp, AttachmentStore store, Dictionary<string, AttachmentMetadata> attachmentMap)
    {
        return sp.Type switch
        {
            nameof(ChatMessageTypes.Text) =>
                !string.IsNullOrEmpty(sp.Text) ? new ChatMessagePart(sp.Text) : null,

            nameof(ChatMessageTypes.Image) =>
                DeserializeImagePartFull(sp, store, attachmentMap),

            nameof(ChatMessageTypes.Audio) =>
                DeserializeAudioPartFull(sp, store, attachmentMap),

            nameof(ChatMessageTypes.Document) =>
                DeserializeDocumentPartFull(sp, store, attachmentMap),

            nameof(ChatMessageTypes.Reasoning) =>
                DeserializeReasoningPart(sp),

            _ => !string.IsNullOrEmpty(sp.Text) ? new ChatMessagePart(sp.Text) : null,
        };
    }

    private static ChatMessagePart? DeserializeImagePartLightweight(SerializedPart sp)
    {
        if (string.IsNullOrEmpty(sp.ImageUrl)) return null;

        // For attachment refs, create an image part with the ref as URL (UI will lazy-load)
        ImageDetail detail = ParseImageDetail(sp.ImageDetail);
        return new ChatMessagePart(sp.ImageUrl, detail, sp.ImageMimeType);
    }

    private static ChatMessagePart? DeserializeImagePartFull(
        SerializedPart sp, AttachmentStore store, Dictionary<string, AttachmentMetadata> attachmentMap)
    {
        if (string.IsNullOrEmpty(sp.ImageUrl)) return null;

        if (TryGetAttachmentId(sp.ImageUrl, out string? attId) && attachmentMap.TryGetValue(attId, out AttachmentMetadata? meta))
        {
            byte[]? bytes = store.LoadAttachment(meta.StoragePath);
            if (bytes is not null)
            {
                string base64 = Convert.ToBase64String(bytes);
                string dataUri = $"data:{meta.MimeType};base64,{base64}";
                ImageDetail detail = ParseImageDetail(sp.ImageDetail);
                return new ChatMessagePart(dataUri, detail, meta.MimeType);
            }
        }

        // Fallback: URL or unresolved ref
        if (Uri.TryCreate(sp.ImageUrl, UriKind.Absolute, out Uri? uri))
            return new ChatMessagePart(uri);

        return new ChatMessagePart(sp.ImageUrl, ParseImageDetail(sp.ImageDetail), sp.ImageMimeType);
    }

    private static ChatMessagePart? DeserializeAudioPartLightweight(SerializedPart sp)
    {
        if (!string.IsNullOrEmpty(sp.AudioUrl) && Uri.TryCreate(sp.AudioUrl, UriKind.Absolute, out Uri? uri))
            return new ChatMessagePart(uri, ChatMessageTypes.Audio);

        // For attachment refs, create a placeholder
        if (!string.IsNullOrEmpty(sp.AudioRef))
        {
            Enum.TryParse<ChatAudioFormats>(sp.AudioFormat, true, out ChatAudioFormats fmt);
            // Return a minimal audio part; actual data loaded on demand
            return new ChatMessagePart(ChatMessageTypes.Audio);
        }

        return null;
    }

    private static ChatMessagePart? DeserializeAudioPartFull(
        SerializedPart sp, AttachmentStore store, Dictionary<string, AttachmentMetadata> attachmentMap)
    {
        if (!string.IsNullOrEmpty(sp.AudioRef) &&
            TryGetAttachmentId(sp.AudioRef, out string? attId) &&
            attachmentMap.TryGetValue(attId, out AttachmentMetadata? meta))
        {
            byte[]? bytes = store.LoadAttachment(meta.StoragePath);
            if (bytes is not null)
            {
                Enum.TryParse<ChatAudioFormats>(sp.AudioFormat, true, out ChatAudioFormats fmt);
                return new ChatMessagePart(bytes, fmt);
            }
        }

        if (!string.IsNullOrEmpty(sp.AudioUrl) && Uri.TryCreate(sp.AudioUrl, UriKind.Absolute, out Uri? uri))
            return new ChatMessagePart(uri, ChatMessageTypes.Audio);

        return null;
    }

    private static ChatMessagePart? DeserializeDocumentPartLightweight(SerializedPart sp)
    {
        if (!string.IsNullOrEmpty(sp.DocumentUrl) && Uri.TryCreate(sp.DocumentUrl, UriKind.Absolute, out Uri? uri))
            return new ChatMessagePart(new ChatDocument(uri));

        // For attachment refs, create a placeholder
        if (!string.IsNullOrEmpty(sp.DocumentRef))
            return new ChatMessagePart(ChatMessageTypes.Document);

        return null;
    }

    private static ChatMessagePart? DeserializeDocumentPartFull(
        SerializedPart sp, AttachmentStore store, Dictionary<string, AttachmentMetadata> attachmentMap)
    {
        if (!string.IsNullOrEmpty(sp.DocumentRef) &&
            TryGetAttachmentId(sp.DocumentRef, out string? attId) &&
            attachmentMap.TryGetValue(attId, out AttachmentMetadata? meta))
        {
            byte[]? bytes = store.LoadAttachment(meta.StoragePath);
            if (bytes is not null)
            {
                string base64 = Convert.ToBase64String(bytes);
                return new ChatMessagePart(new ChatDocument(base64));
            }
        }

        if (!string.IsNullOrEmpty(sp.DocumentUrl) && Uri.TryCreate(sp.DocumentUrl, UriKind.Absolute, out Uri? uri))
            return new ChatMessagePart(new ChatDocument(uri));

        return null;
    }

    private static ChatMessagePart? DeserializeReasoningPart(SerializedPart sp)
    {
        if (string.IsNullOrEmpty(sp.ReasoningContent) && string.IsNullOrEmpty(sp.ReasoningSignature))
            return null;

        return new ChatMessagePart(new ChatMessageReasoningData
        {
            Content = sp.ReasoningContent,
            Signature = sp.ReasoningSignature,
        });
    }

    private static ChatMessageRoles ParseRole(string role) => role.ToLowerInvariant() switch
    {
        "system" => ChatMessageRoles.System,
        "assistant" => ChatMessageRoles.Assistant,
        "user" => ChatMessageRoles.User,
        _ => ChatMessageRoles.User,
    };

    private static ImageDetail ParseImageDetail(string? detail) => detail?.ToLowerInvariant() switch
    {
        "low" => ImageDetail.Low,
        "high" => ImageDetail.High,
        _ => ImageDetail.Auto,
    };

    /// <summary>
    /// Try to extract an attachment ID from an <c>attachment:{id}</c> reference string.
    /// </summary>
    internal static bool TryGetAttachmentId(string value, out string? attachmentId)
    {
        if (value.StartsWith("attachment:", StringComparison.Ordinal) && value.Length > 11)
        {
            attachmentId = value[11..];
            return true;
        }
        attachmentId = null;
        return false;
    }

    private static bool IsBase64Content(string value)
    {
        return value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
               || (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                   && value.Length > 256);
    }

    private static byte[] ExtractBase64Bytes(string value)
    {
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int commaIndex = value.IndexOf(',');
            if (commaIndex >= 0)
                return Convert.FromBase64String(value[(commaIndex + 1)..]);
        }
        return Convert.FromBase64String(value);
    }

    private static string? GuessMimeFromDataUri(string dataUri)
    {
        // data:image/png;base64,... → image/png
        if (!dataUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;
        int semicolon = dataUri.IndexOf(';');
        return semicolon > 5 ? dataUri[5..semicolon] : null;
    }

    private static string GetMimeForAudioFormat(ChatAudioFormats? format) => format switch
    {
        ChatAudioFormats.Wav => "audio/wav",
        ChatAudioFormats.Mp3 => "audio/mpeg",
        _ => "audio/wav",
    };
}
