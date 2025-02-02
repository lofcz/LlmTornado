using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
/// URI-based file data.
/// </summary>
public class ChatMessagePartFileLinkData
{
    /// <summary>
    /// MIME type of the file.
    /// </summary>
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// URI of the file.
    /// </summary>
    [JsonProperty("fileUri")]
    public string FileUri { get; set; }

    /// <summary>
    /// Creates a new file link data, which can be used for constructing a message part.
    /// </summary>
    /// <param name="fileUri"></param>
    /// <param name="mimeType"></param>
    public ChatMessagePartFileLinkData(string fileUri, string? mimeType = null)
    {
        FileUri = fileUri;
        MimeType = mimeType;
    }
}