using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Tokenize.Vendors;
using Newtonsoft.Json;

namespace LlmTornado.Tokenize;

/// <summary>
///     Represents a tokenize result returned by the Tokenize API.
/// </summary>
public class TokenizeResult
{
    /// <summary>
    ///     Total number of tokens.
    /// </summary>
    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    ///     The native/vendor-specific tokenize result. This provides access to provider-specific properties.
    /// </summary>
    [JsonIgnore]
    public IVendorTokenizeResult? NativeResult { get; set; }

    internal static TokenizeResult? Deserialize(LLmProviders provider, string jsonData, string? postData)
    {
        return provider switch
        {
            LLmProviders.OpenAi => JsonConvert.DeserializeObject<VendorOpenAiTokenizeResult>(jsonData)?.ToResult(),
            LLmProviders.MoonshotAi => JsonConvert.DeserializeObject<VendorMoonshotAiTokenizeResult>(jsonData)?.ToResult(),
            LLmProviders.Anthropic => JsonConvert.DeserializeObject<VendorAnthropicTokenizeResult>(jsonData)?.ToResult(),
            LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleTokenizeResult>(jsonData)?.ToResult(),
            LLmProviders.Cohere => JsonConvert.DeserializeObject<VendorCohereTokenizeResult>(jsonData)?.ToResult(),
            _ => JsonConvert.DeserializeObject<TokenizeResult>(jsonData)
        };
    }
}

