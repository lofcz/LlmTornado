using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LlmTornado.Skills;

/// <summary>
/// Represents a skill that can be used with Claude.
/// </summary>
public class Skill
{
    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
    
    /// <summary>
    /// The type of object. Always "skill".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill";
    
    /// <summary>
    /// The name of the skill.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// A description of what the skill does.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// The ID of the currently active version for this skill.
    /// </summary>
    [JsonProperty("active_version_id")]
    public string? ActiveVersionId { get; set; }
    
    /// <summary>
    /// The timestamp when the skill was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the skill was last updated.
    /// </summary>
    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a version of a skill.
/// </summary>
public class SkillVersion
{
    /// <summary>
    /// Unique identifier for the skill version.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
    
    /// <summary>
    /// The type of object. Always "skill_version".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill_version";
    
    /// <summary>
    /// The ID of the skill this version belongs to.
    /// </summary>
    [JsonProperty("skill_id")]
    public string SkillId { get; set; }
    
    /// <summary>
    /// The system prompt for this skill version.
    /// </summary>
    [JsonProperty("system_prompt")]
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// Optional metadata about this version.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    
    /// <summary>
    /// The timestamp when the version was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Response containing a list of skills.
/// </summary>
public class SkillListResponse
{
    /// <summary>
    /// The type of object. Always "list".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "list";
    
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
    /// The first ID in this page of results.
    /// </summary>
    [JsonProperty("first_id")]
    public string? FirstId { get; set; }
    
    /// <summary>
    /// The last ID in this page of results.
    /// </summary>
    [JsonProperty("last_id")]
    public string? LastId { get; set; }
}

/// <summary>
/// Response containing a list of skill versions.
/// </summary>
public class SkillVersionListResponse
{
    /// <summary>
    /// The type of object. Always "list".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "list";
    
    /// <summary>
    /// The list of skill versions.
    /// </summary>
    [JsonProperty("data")]
    public List<SkillVersion> Data { get; set; } = new();
    
    /// <summary>
    /// Whether there are more results available.
    /// </summary>
    [JsonProperty("has_more")]
    public bool HasMore { get; set; }
    
    /// <summary>
    /// The first ID in this page of results.
    /// </summary>
    [JsonProperty("first_id")]
    public string? FirstId { get; set; }
    
    /// <summary>
    /// The last ID in this page of results.
    /// </summary>
    [JsonProperty("last_id")]
    public string? LastId { get; set; }
}
