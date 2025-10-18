using Newtonsoft.Json;

namespace LlmTornado.Skills;

public class SkillVersionDeleteResponse
{
    /// <summary>
    /// Type of object. Always "skill_deleted".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill_version_deleted";

    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}
