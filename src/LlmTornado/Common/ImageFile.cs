using LlmTornado.Images;
using Newtonsoft.Json;

namespace LlmTornado.Common;

/// <summary>
///     Object that represents ImageFile
/// </summary>
public sealed class ImageFile
{
    /// <summary>
    ///     The file ID of the image.
    /// </summary>
    [JsonProperty("file_id")]
    public string FileId { get; set; } = null!;

    /// <summary>
    ///     Specifies the detail level of the image if specified by the user.
    ///     low uses fewer tokens, you can opt in to high resolution using high.
    /// </summary>
    [JsonProperty("detail")]
    public ImageDetail Detail { get; set; }
}