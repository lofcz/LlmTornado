using System.Collections.Generic;
using LlmTornado.Videos;
using Newtonsoft.Json;

namespace LlmTornado.Videos.Vendors.OpenAi;

/// <summary>
/// OpenAI-specific extensions for video generation requests.
/// </summary>
public class VideoOpenAiExtensions
{
    /// <summary>
    /// Reusable character references to include in the generation (up to two).
    /// Requires JSON request body; use with <see cref="UseJsonContent"/> or when characters are set.
    /// </summary>
    [JsonProperty("characters")]
    public List<VideoCharacterReference>? Characters { get; set; }

    /// <summary>
    /// JSON input reference for image-guided generation (Batch API and JSON requests).
    /// Provide exactly one of <see cref="VideoInputReference.FileId"/> or <see cref="VideoInputReference.ImageUrl"/>.
    /// </summary>
    [JsonProperty("input_reference")]
    public VideoInputReference? InputReference { get; set; }

    /// <summary>
    /// Forces JSON request body instead of multipart/form-data.
    /// Automatically enabled when <see cref="Characters"/> is set.
    /// </summary>
    [JsonIgnore]
    public bool UseJsonContent { get; set; }
}
