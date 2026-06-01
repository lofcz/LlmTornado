using Newtonsoft.Json;

namespace LlmTornado.Skills;

/// <summary>
/// OpenAI skill object returned by <c>/v1/skills</c>.
/// </summary>
public class OpenAiSkill
{
    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp (seconds) for when the skill was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAtUnix { get; set; }

    /// <summary>
    /// Default version for the skill.
    /// </summary>
    [JsonProperty("default_version")]
    public string? DefaultVersion { get; set; }

    /// <summary>
    /// Description extracted from the skill manifest.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Latest version for the skill.
    /// </summary>
    [JsonProperty("latest_version")]
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Name of the skill.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Object type. Always <c>skill</c>.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "skill";
}
