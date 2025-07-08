using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Uploads;

/// <summary>
///     Request body for creating an Upload.
/// </summary>
public class CreateUploadRequest
{
    /// <summary>
    ///     The number of bytes in the file you are uploading.
    /// </summary>
    [JsonProperty("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    ///     The name of the file to upload. This must fall within the supported MIME types for your file purpose. See the supported MIME types for assistants and vision.
    /// </summary>
    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     The MIME type of the file.
    /// </summary>
    [JsonProperty("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    ///     The intended purpose of the uploaded file.
    /// </summary>
    [JsonProperty("purpose")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UploadPurpose Purpose { get; set; } = UploadPurpose.UserData;
} 