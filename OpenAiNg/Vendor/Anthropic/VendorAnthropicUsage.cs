using Newtonsoft.Json;

namespace OpenAiNg.Vendor.Anthropic;

internal class VendorAnthropicUsage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }
    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
}