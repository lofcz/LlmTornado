using Newtonsoft.Json;

namespace LlmTornado.Skills;

/// <summary>
/// Response when deleting an OpenAI skill.
/// </summary>
public class OpenAiSkillDeleted
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("deleted")]
    public bool Deleted { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; } = "skill.deleted";
}
