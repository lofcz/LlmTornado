using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Options for streaming responses. Only set this when <c>stream</c> is <c>true</c>.
/// </summary>
public class ResponseStreamOptions
{
    /// <summary>
    /// When true, stream obfuscation will be enabled. Stream obfuscation adds
    /// random characters to an <c>obfuscation</c> field on streaming delta events to
    /// normalize payload sizes as a mitigation to certain side-channel attacks.
    /// These obfuscation fields are included by default, but add a small amount
    /// of overhead to the data stream. You can set this to <c>false</c> to optimize
    /// for bandwidth if you trust the network links between your application and
    /// the OpenAI API.
    /// </summary>
    [JsonProperty("include_obfuscation")]
    public bool? IncludeObfuscation { get; set; }

    public ResponseStreamOptions() { }

    public ResponseStreamOptions(bool? includeObfuscation)
    {
        IncludeObfuscation = includeObfuscation;
    }
}
