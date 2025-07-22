using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Uploads;

/// <summary>
///     Request body for completing an Upload.
/// </summary>
public class CompleteUploadRequest
{
    /// <summary>
    ///     Ordered list of part IDs.
    /// </summary>
    [JsonProperty("part_ids")]
    public List<string> PartIds { get; set; } = [];

    /// <summary>
    ///     Optional MD5 checksum for the entire file.
    /// </summary>
    [JsonProperty("md5")]
    public string? Md5 { get; set; }
} 