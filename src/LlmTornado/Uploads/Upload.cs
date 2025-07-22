using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using LlmTornado.Files;

namespace LlmTornado.Uploads;

/// <summary>
///     The Upload object can accept byte chunks in the form of Parts.
/// </summary>
public class Upload
{
    /// <summary>
    ///     The Upload unique identifier, which can be referenced in API endpoints.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     The object type, which is always "upload".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "upload";

    /// <summary>
    ///     The intended number of bytes to be uploaded.
    /// </summary>
    [JsonProperty("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the Upload was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; set; }
    
    /// <summary>
    ///     Date the upload was created.
    /// </summary>
    [JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     The name of the file to be uploaded.
    /// </summary>
    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     The intended purpose of the file.
    /// </summary>
    [JsonProperty("purpose")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UploadPurpose Purpose { get; set; }

    /// <summary>
    ///     The status of the Upload.
    /// </summary>
    [JsonProperty("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UploadStatus Status { get; set; }

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the Upload will expire.
    /// </summary>
    [JsonProperty("expires_at")]
    public int ExpiresAtUnixTimeSeconds { get; set; }

    /// <summary>
    ///     Date the upload expires.
    /// </summary>
    [JsonIgnore]
    public DateTime ExpiresAt => DateTimeOffset.FromUnixTimeSeconds(ExpiresAtUnixTimeSeconds).DateTime;
    
    /// <summary>
    ///     The ready File object after the Upload is completed.
    /// </summary>
    [JsonProperty("file")]
    public TornadoFile? File { get; set; }
}