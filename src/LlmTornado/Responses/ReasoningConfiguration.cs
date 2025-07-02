using System;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Configuration options for reasoning models (o-series models only)
/// </summary>
public class ReasoningConfiguration
{
    /// <summary>
    /// Constrains effort on reasoning for reasoning models.
    /// </summary>
    [JsonProperty("effort")]
    public ResponseReasoningEfforts? Effort { get; set; }

    /// <summary>
    /// A summary of the reasoning performed by the model. This can be useful for debugging and understanding the model's reasoning process.
    /// </summary>
    [JsonProperty("summary")]
    public ResponseReasoningSummaries? Summary { get; set; }

    public ReasoningConfiguration() { }

    public ReasoningConfiguration(ResponseReasoningEfforts? effort = null, ResponseReasoningSummaries? summary = null)
    {
        Effort = effort;
        Summary = summary;
    }
}

 