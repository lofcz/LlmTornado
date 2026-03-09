using Newtonsoft.Json;

namespace LlmTornado.Tokenize.Vendors;

internal class VendorOpenAiTokenizeResult : IVendorTokenizeResult
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    public TokenizeResult ToResult()
    {
        return new TokenizeResult
        {
            TotalTokens = InputTokens,
            NativeResult = this
        };
    }
}
