using Newtonsoft.Json;

namespace LlmTornado.Vendor.Anthropic;

internal class VendorGoogleUsage
{
    [JsonProperty("promptTokenCount")]
    public int PromptTokenCount { get; set; }
    [JsonProperty("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }
    [JsonProperty("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}