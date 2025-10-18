using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace LlmTornado.Skills;

/// <summary>
/// Represents a skill that can be used with Claude.
/// </summary>
public class Skill
{
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }
    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }


    /// <summary>
    /// "custom": the skill was created by a user
    /// "anthropic": the skill was created by Anthropic
    /// </summary>
    [JsonProperty("source")]
    public string Source { get; set; } = "anthropic";

    /// <summary>
    /// The type of object. Always "skill".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill";
    
    /// <summary>
    /// The ID of the currently active version for this skill.
    /// </summary>
    [JsonProperty("latest_version")]
    public string LatestVersion { get; set; }
    
    /// <summary>
    /// The timestamp when the skill was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the skill was last updated.
    /// </summary>
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

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

public class SkillDeleteResponse
{
    /// <summary>
    /// Type of object. Always "skill_deleted".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill_deleted";

    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}

/// <summary>
/// Response containing a list of skills.
/// </summary>
public class SkillListResponse
{ 
    /// <summary>
    /// The list of skills.
    /// </summary>
    [JsonProperty("data")]
    public List<Skill> Data { get; set; } = new();
    
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
