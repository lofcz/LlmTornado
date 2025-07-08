using System;
using Newtonsoft.Json;

namespace LlmTornado.Uploads;

/// <summary>
///     The upload Part represents a chunk of bytes that can add to an Upload object.
/// </summary>
public class UploadPart
{
    /// <summary>
    ///     The upload Part unique identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     The object type, always `upload.part`.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "upload.part";

    /// <summary>
    ///     The Unix timestamp (in seconds) for when the Part was created.
    /// </summary>
    [JsonProperty("created_at")]
    public int CreatedAtUnixTimeSeconds { get; set; }
    
    /// <summary>
    ///     Date the part was created.
    /// </summary>
    [JsonIgnore]
    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

    /// <summary>
    ///     The ID of the Upload object that this Part was added to.
    /// </summary>
    [JsonProperty("upload_id")]
    public string UploadId { get; set; } = string.Empty;
} 