using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Common;

public sealed class ImageFile
{
    /// <summary>
    ///     The file ID of the image.
    /// </summary>
    [JsonInclude]
    [JsonProperty("file_id")]
    public string? FileId { get; private set; }
}