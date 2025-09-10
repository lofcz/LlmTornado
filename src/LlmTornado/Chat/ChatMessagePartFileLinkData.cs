using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Files;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
/// A content block that represents a file to be uploaded to the container. Files uploaded via this block will be available in the container's input directory.
/// </summary>
public class ChatMessagePartContainerUpload
{
    /// <summary>
    /// File ID.
    /// </summary>
    [JsonProperty("file_id")]
    public string FileId { get; set; }
    
    /// <summary>
    /// Cache control settings.
    /// </summary>
    [JsonProperty("cache_control")]
    public AnthropicCacheSettings? Cache { get; set; }
    
    /// <summary>
    /// Creates a new container upload data.<br/>
    /// </summary>
    public ChatMessagePartContainerUpload(string fileId)
    {
        FileId = fileId;
    }
    
    /// <summary>
    /// Creates a new container upload data.<br/>
    /// </summary>
    public ChatMessagePartContainerUpload(TornadoFile file)
    {
        FileId = file.Uri ?? file.Id;
    }
}

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
    /// State of the file
    /// </summary>
    [JsonIgnore]
    public FileLinkStates? State { get; set; }
    
    /// <summary>
    /// File from which this part was created.
    /// </summary>
    [JsonIgnore]
    public TornadoFile? File { get; set; }

    /// <summary>
    /// Creates a new file link data, which can be used for constructing a message part.<br/>
    /// Note: For Gemini 2.0+ this can be a YouTube url too
    /// </summary>
    /// <param name="fileUri"></param>
    /// <param name="mimeType"></param>
    public ChatMessagePartFileLinkData(string fileUri, string? mimeType = null)
    {
        FileUri = fileUri;
        MimeType = mimeType;
    }
    
    /// <summary>
    /// Creates a new file link data from a file. This passes the state of the file, as well as the link and mime type.
    /// Note: For Gemini 2.0+ this can be a YouTube url too
    /// </summary>
    /// <param name="file"></param>
    public ChatMessagePartFileLinkData(TornadoFile file)
    {
        FileUri = file.Uri ?? string.Empty;
        MimeType = file.MimeType ?? string.Empty;
        State = file.State;
        File = file;
    }
}

/// <summary>
/// States of file link data
/// </summary>
public enum FileLinkStates
{
    /// <summary>
    /// 	The default value. This value is used if the state is omitted.
    /// </summary>
    Unknown,
    
    /// <summary>
    ///     File is being processed and cannot be used for inference yet.
    /// </summary>
    Processing,
    
    /// <summary>
    /// 	File is processed and available for inference.
    /// </summary>
    Active,
    
    /// <summary>
    ///     File failed processing.
    /// </summary>
    Failed
}