using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.RateLimits;

/// <summary>
/// Rate limit group categories returned by the Anthropic Admin Rate Limits API.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicRateLimitGroupType
{
    /// <summary>Messages API model family limits.</summary>
    [EnumMember(Value = "model_group")]
    ModelGroup,

    /// <summary>Message Batches API limits.</summary>
    [EnumMember(Value = "batch")]
    Batch,

    /// <summary>Token counting API limits.</summary>
    [EnumMember(Value = "token_count")]
    TokenCount,

    /// <summary>Files API limits.</summary>
    [EnumMember(Value = "files")]
    Files,

    /// <summary>Agent skills limits.</summary>
    [EnumMember(Value = "skills")]
    Skills,

    /// <summary>Web search tool limits.</summary>
    [EnumMember(Value = "web_search")]
    WebSearch
}
