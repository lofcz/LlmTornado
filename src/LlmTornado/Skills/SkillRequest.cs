using Newtonsoft.Json;
using System.Collections.Generic;

namespace LlmTornado.Skills;

/// <summary>
/// Request to create a new skill.
/// </summary>
public class CreateSkillRequest
{
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
    /// Creates a new create skill request.
    /// </summary>
    public CreateSkillRequest()
    {
    }
    
    /// <summary>
    /// Creates a new create skill request with a name.
    /// </summary>
    /// <param name="name">The name of the skill</param>
    public CreateSkillRequest(string name)
    {
        Name = name;
    }
    
    /// <summary>
    /// Creates a new create skill request with a name and description.
    /// </summary>
    /// <param name="name">The name of the skill</param>
    /// <param name="description">A description of what the skill does</param>
    public CreateSkillRequest(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

/// <summary>
/// Request to update a skill.
/// </summary>
public class UpdateSkillRequest
{
    /// <summary>
    /// The name of the skill.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// A description of what the skill does.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// The ID of the active version for this skill.
    /// </summary>
    [JsonProperty("active_version_id")]
    public string? ActiveVersionId { get; set; }
}

/// <summary>
/// Request to create a new skill version.
/// </summary>
public class CreateSkillVersionRequest
{
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
    /// Creates a new create skill version request.
    /// </summary>
    public CreateSkillVersionRequest()
    {
    }
    
    /// <summary>
    /// Creates a new create skill version request with a system prompt.
    /// </summary>
    /// <param name="systemPrompt">The system prompt for this version</param>
    public CreateSkillVersionRequest(string systemPrompt)
    {
        SystemPrompt = systemPrompt;
    }
}
