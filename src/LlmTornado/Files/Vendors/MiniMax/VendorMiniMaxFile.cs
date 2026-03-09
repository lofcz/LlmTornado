using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.MiniMax;

/// <summary>
/// MiniMax file object shared across upload, list, retrieve responses.
/// </summary>
internal class VendorMiniMaxFileObject
{
    [JsonProperty("file_id")]
    public long FileId { get; set; }
    
    [JsonProperty("bytes")]
    public long Bytes { get; set; }
    
    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }
    
    [JsonProperty("filename")]
    public string? Filename { get; set; }
    
    [JsonProperty("purpose")]
    public string? Purpose { get; set; }
    
    [JsonProperty("download_url")]
    public string? DownloadUrl { get; set; }
    
    public TornadoFile ToFile()
    {
        return new TornadoFile
        {
            Id = FileId.ToString(),
            Name = Filename,
            Bytes = Bytes,
            CreatedAt = CreatedAt,
            Object = "file",
            Status = "uploaded",
            DownloadUrl = DownloadUrl
        };
    }
}

internal class VendorMiniMaxFileBaseResp
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }
    
    [JsonProperty("status_msg")]
    public string? StatusMsg { get; set; }
}

/// <summary>
/// Response from MiniMax file upload (POST /v1/files/upload).
/// </summary>
internal class VendorMiniMaxUploadResponse
{
    [JsonProperty("file")]
    public VendorMiniMaxFileObject? File { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxFileBaseResp? BaseResp { get; set; }
}

/// <summary>
/// Response from MiniMax file list (GET /v1/files/list).
/// </summary>
internal class VendorMiniMaxListFilesResponse
{
    [JsonProperty("files")]
    public List<VendorMiniMaxFileObject>? Files { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxFileBaseResp? BaseResp { get; set; }
    
    public TornadoPagingList<TornadoFile> ToList()
    {
        return new TornadoPagingList<TornadoFile>
        {
            Items = Files?.Select(f => f.ToFile()).ToList() ?? []
        };
    }
}

/// <summary>
/// Response from MiniMax file retrieve (GET /v1/files/retrieve).
/// </summary>
internal class VendorMiniMaxRetrieveFileResponse
{
    [JsonProperty("file")]
    public VendorMiniMaxFileObject? File { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxFileBaseResp? BaseResp { get; set; }
}

/// <summary>
/// Request body for MiniMax file delete (POST /v1/files/delete).
/// </summary>
internal class VendorMiniMaxDeleteFileRequest
{
    [JsonProperty("file_id")]
    public long FileId { get; set; }
    
    [JsonProperty("purpose")]
    public string? Purpose { get; set; }
}

/// <summary>
/// Response from MiniMax file delete (POST /v1/files/delete).
/// </summary>
internal class VendorMiniMaxDeleteFileResponse
{
    [JsonProperty("file_id")]
    public long FileId { get; set; }
    
    [JsonProperty("base_resp")]
    public VendorMiniMaxFileBaseResp? BaseResp { get; set; }
}
