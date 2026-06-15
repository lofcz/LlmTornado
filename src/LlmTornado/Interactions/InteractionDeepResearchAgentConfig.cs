using Newtonsoft.Json;

namespace LlmTornado.Interactions;

/// <summary>
/// Agent configuration for Gemini Deep Research managed agents.
/// </summary>
public class InteractionDeepResearchAgentConfig
{
    /// <summary>
    /// Must be <c>deep-research</c>.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "deep-research";

    /// <summary>
    /// Set to <c>auto</c> to receive intermediate reasoning steps during streaming; <c>none</c> to disable.
    /// </summary>
    [JsonProperty("thinking_summaries", NullValueHandling = NullValueHandling.Ignore)]
    public string? ThinkingSummaries { get; set; }

    /// <summary>
    /// Set to <c>auto</c> to enable agent-generated charts and images; <c>off</c> to disable.
    /// </summary>
    [JsonProperty("visualization", NullValueHandling = NullValueHandling.Ignore)]
    public string? Visualization { get; set; }

    /// <summary>
    /// When <c>true</c>, the agent returns a research plan instead of executing immediately.
    /// </summary>
    [JsonProperty("collaborative_planning", NullValueHandling = NullValueHandling.Ignore)]
    public bool? CollaborativePlanning { get; set; }

    /// <summary>
    /// Default Deep Research configuration with thinking summaries and visualization enabled.
    /// </summary>
    public static InteractionDeepResearchAgentConfig Default { get; } = new()
    {
        ThinkingSummaries = "auto",
        Visualization = "auto",
        CollaborativePlanning = false
    };

    /// <summary>
    /// Configuration for collaborative planning mode (returns a plan before research).
    /// </summary>
    public static InteractionDeepResearchAgentConfig Planning { get; } = new()
    {
        ThinkingSummaries = "auto",
        Visualization = "auto",
        CollaborativePlanning = true
    };
}
