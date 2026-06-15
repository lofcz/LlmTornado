using Newtonsoft.Json;

namespace LlmTornado.Interactions;

/// <summary>
/// Error payload on a failed interaction.
/// </summary>
public class InteractionError
{
    [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; set; }

    [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
    public string? Code { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }
}
