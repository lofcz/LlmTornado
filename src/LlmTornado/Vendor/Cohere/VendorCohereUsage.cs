using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Vendor.Anthropic;

internal class VendorCohereUsage : IChatUsage
{
    internal class VendorCohereUsageApi
    {
        [JsonProperty("version")]
        public int Version { get; set; }
    }

    internal class VendorCohereUsageTokens
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }
        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }
    
    [JsonProperty("api_version")]
    public VendorCohereUsageApi Api { get; set; }
    [JsonProperty("billed_units")]
    public VendorCohereUsageTokens? BilledUnits { get; set; }
    [JsonProperty("tokens")]
    public VendorCohereUsageTokens? Tokens { get; set; }
    [JsonProperty("response_type")]
    public string? ResponseType { get; set; }
}