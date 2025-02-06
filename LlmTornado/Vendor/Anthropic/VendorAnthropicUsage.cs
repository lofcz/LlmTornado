using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Vendor.Anthropic;

internal class VendorAnthropicUsage : IChatUsage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }
    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
    [JsonProperty("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }
    [JsonProperty("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }
}