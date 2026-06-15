using Newtonsoft.Json;

namespace LlmTornado.Skills;

/// <summary>
/// OpenAI skill version object returned by <c>/v1/skills/{skill_id}/versions</c>.
/// </summary>
public class OpenAiSkillVersion
{
    /// <summary>
    /// Unique identifier for the skill version.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unix timestamp (seconds) for when the version was created.
    /// </summary>
    [JsonProperty("created_at")]
    public long CreatedAtUnix { get; set; }

    /// <summary>
    /// Description extracted from the skill manifest.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Name of the skill version.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Object type. Always <c>skill.version</c>.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = "skill.version";

    /// <summary>
    /// Identifier of the parent skill.
    /// </summary>
    [JsonProperty("skill_id")]
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    /// Version number for this skill.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;
}
