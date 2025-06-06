using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents the specific usage metrics related to a run operation.
/// Inherits properties from the <see cref="Usage"/> class to provide token usage statistics.
/// </summary>
public class RunUsage : Usage
{
    /// <summary>
    ///     Number of completion tokens used over the course of the run step.
    /// </summary>
    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }
}