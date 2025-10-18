using System;
using LlmTornado.Skills;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

public class VendorAnthropicChatResultContainer
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("skills")]
    public VendorAnthropicChatResultContainerSkill[] Skills { get; set; }
}

public class VendorAnthropicChatResultContainerSkill
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("skill_id")]
    public string SkillId { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }
}