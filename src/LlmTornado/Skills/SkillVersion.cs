using Newtonsoft.Json;
using System;

namespace LlmTornado.Skills;

/// <summary>
/// Represents a version of a skill.
/// </summary>
public class SkillVersion
{
    [JsonProperty("name")]
    public string Name { get; set; }
    /// <summary>
    /// Unique identifier for the skill version.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("skill_id")]
    public string SkillId { get; set; }

    /// <summary>
    /// Description of the skill version.
    /// This is extracted from the SKILL.md file in the skill upload.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }
    /// <summary>
    /// Directory name of the skill version.
    /// This is the top-level directory name that was extracted from the uploaded files
    /// </summary>
    [JsonProperty("directory")]
    public string Directory { get; set; }

    /// <summary>
    /// The type of object. Always "skill".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill_version";

    /// <summary>
    /// The ID of the currently active version for this skill.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; }

    /// <summary>
    /// The timestamp when the skill was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}
