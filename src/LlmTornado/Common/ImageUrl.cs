using LlmTornado.Images;
using Newtonsoft.Json;

namespace LlmTornado.Common;

/// <summary>
///     Object that represents ImageFile
/// </summary>
public sealed class ImageUrl
{
    /// <summary>
    ///     The url of the image.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; private set; }
    
    /// <summary>
    ///     Specifies the detail level of the image.
    ///     low uses fewer tokens, you can opt in to high resolution using high.
    ///     Default value is auto
    /// </summary>
    [JsonProperty("detail")]
    public ImageDetail? Detail { get; private set; }
}