using Newtonsoft.Json;
using System.Collections.Generic;

namespace LlmTornado.Skills;

/// <summary>
/// Response containing a list of skills.
/// </summary>
public class SkillVersionListResponse
{
    /// <summary>
    /// The list of skills.
    /// </summary>
    [JsonProperty("data")]
    public List<SkillVersion> Data { get; set; } = new();

    /// <summary>
    /// Whether there are more results available.
    /// </summary>
    [JsonProperty("has_more")]
    public bool HasMore { get; set; }

    /// <summary>
    /// The next page of results.
    /// </summary>
    [JsonProperty("next_page")]
    public string? NextPage { get; set; }

}
