using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Skills;

/// <summary>
/// Paginated list of OpenAI skills.
/// </summary>
public class OpenAiSkillList
{
    [JsonProperty("data")]
    public List<OpenAiSkill> Data { get; set; } = [];

    [JsonProperty("first_id")]
    public string? FirstId { get; set; }

    [JsonProperty("has_more")]
    public bool HasMore { get; set; }

    [JsonProperty("last_id")]
    public string? LastId { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; } = "list";
}
