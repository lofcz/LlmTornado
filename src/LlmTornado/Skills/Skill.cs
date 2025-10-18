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
