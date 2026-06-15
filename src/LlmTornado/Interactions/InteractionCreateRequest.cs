using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Webhooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// Request body for <c>POST /v1beta/interactions</c>.
/// </summary>
public class InteractionCreateRequest
{
    /// <summary>
    /// Interactions API schema revision sent via <c>Api-Revision</c> header.
    /// Defaults to the May 2026 steps schema.
    /// </summary>
    [JsonIgnore]
    public InteractionSchemaRevision ApiRevision { get; set; } = InteractionSchemaRevision.May2026;

    /// <summary>
    /// Gemini model name. Required if <see cref="Agent"/> is not set.
    /// </summary>
    [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
    public string? Model { get; set; }

    /// <summary>
    /// Managed agent ID (e.g. <see cref="GoogleManagedAgentIds.AntigravityPreview052026"/>).
    /// Required if <see cref="Model"/> is not set.
    /// </summary>
    [JsonProperty("agent", NullValueHandling = NullValueHandling.Ignore)]
    public string? Agent { get; set; }

    /// <summary>
    /// User input: text, content blocks, or prior steps.
    /// </summary>
    [JsonProperty("input")]
    public InteractionInput Input { get; set; } = InteractionInput.FromText(string.Empty);

    /// <summary>
    /// Remote sandbox: <c>remote</c>, existing environment ID, or full config.
    /// </summary>
    [JsonProperty("environment", NullValueHandling = NullValueHandling.Ignore)]
    public InteractionEnvironmentReference? Environment { get; set; }

    /// <summary>
    /// Continue conversation context from a prior interaction.
    /// </summary>
    [JsonProperty("previous_interaction_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? PreviousInteractionId { get; set; }

    /// <summary>
    /// System instruction for this interaction.
    /// </summary>
    [JsonProperty("system_instruction", NullValueHandling = NullValueHandling.Ignore)]
    public string? SystemInstruction { get; set; }

    /// <summary>
    /// Tools the agent may use (<c>code_execution</c>, <c>google_search</c>, <c>url_context</c>).
    /// </summary>
    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public List<InteractionTool>? Tools { get; set; }

    /// <summary>
    /// May 2026 polymorphic output format (text/image/audio). Use <see cref="ResponseFormats"/> for multimodal output.
    /// </summary>
    [JsonIgnore]
    public InteractionResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Multiple output modalities for the May 2026 schema.
    /// </summary>
    [JsonIgnore]
    public List<InteractionResponseFormat>? ResponseFormats { get; set; }

    /// <summary>
    /// Legacy JSON schema for structured output (used with <see cref="ResponseMimeType"/> on legacy schema).
    /// </summary>
    [JsonIgnore]
    public JObject? LegacyJsonSchema { get; set; }

    /// <summary>
    /// Legacy response MIME type (removed in May 2026 schema; use <see cref="InteractionResponseFormat.MimeType"/> instead).
    /// </summary>
    [JsonIgnore]
    public string? ResponseMimeType { get; set; }

    /// <summary>
    /// Model behavior configuration (temperature, thinking, etc.).
    /// </summary>
    [JsonIgnore]
    public InteractionGenerationConfig? GenerationConfig { get; set; }

    /// <summary>
    /// Stream step deltas via SSE when <c>true</c>.
    /// </summary>
    [JsonProperty("stream", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Stream { get; set; }

    /// <summary>
    /// Persist the interaction for later retrieval. Required for Antigravity agent.
    /// </summary>
    [JsonProperty("store", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Store { get; set; }

    /// <summary>
    /// Run the interaction in the background (not supported by Antigravity).
    /// </summary>
    [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Background { get; set; }

    /// <summary>
    /// Per-request dynamic webhook configuration for background/LRO interactions.
    /// </summary>
    [JsonProperty("webhook_config", NullValueHandling = NullValueHandling.Ignore)]
    public GeminiWebhookConfig? WebhookConfig { get; set; }

    /// <summary>
    /// Deep Research agent configuration (<c>collaborative_planning</c>, <c>visualization</c>, <c>thinking_summaries</c>).
    /// </summary>
    [JsonIgnore]
    public InteractionDeepResearchAgentConfig? AgentConfig { get; set; }

    /// <summary>
    /// Creates a request for the Antigravity managed agent with recommended defaults.
    /// </summary>
    public static InteractionCreateRequest ForAntigravity(string input, InteractionEnvironmentReference? environment = null)
    {
        return new InteractionCreateRequest
        {
            Agent = GoogleManagedAgentIds.AntigravityPreview052026,
            Input = InteractionInput.FromText(input),
            Environment = environment ?? InteractionEnvironmentReference.Remote,
            Store = true
        };
    }

    /// <summary>
    /// Creates a request for a saved managed agent by ID.
    /// </summary>
    public static InteractionCreateRequest ForManagedAgent(string agentId, string input, InteractionEnvironmentReference? environment = null)
    {
        return new InteractionCreateRequest
        {
            Agent = agentId,
            Input = InteractionInput.FromText(input),
            Environment = environment ?? InteractionEnvironmentReference.Remote,
            Store = true
        };
    }

    /// <summary>
    /// Creates a request for a Gemini model via the Interactions API.
    /// </summary>
    public static InteractionCreateRequest ForModel(ChatModel model, string input)
    {
        return new InteractionCreateRequest
        {
            Model = model.Name,
            Input = InteractionInput.FromText(input),
            Store = true
        };
    }

    /// <summary>
    /// Sets structured JSON output using the May 2026 <c>response_format</c> schema.
    /// </summary>
    public InteractionCreateRequest WithJsonSchema(JObject schema)
    {
        ResponseFormat = InteractionResponseFormat.Json(schema);
        return this;
    }

    /// <summary>
    /// Sets structured JSON output using the legacy <c>response_mime_type</c> + schema format.
    /// </summary>
    public InteractionCreateRequest WithLegacyJsonSchema(JObject schema, string mimeType = "application/json")
    {
        ApiRevision = InteractionSchemaRevision.LegacyMay2026;
        LegacyJsonSchema = schema;
        ResponseMimeType = mimeType;
        return this;
    }

    /// <summary>
    /// Creates a Deep Research agent request with recommended defaults (<c>background</c> and <c>store</c> enabled).
    /// </summary>
    public static InteractionCreateRequest ForDeepResearch(
        InteractionInput input,
        string agent = GoogleManagedAgentIds.DeepResearchPreview042026,
        InteractionDeepResearchAgentConfig? agentConfig = null,
        List<InteractionTool>? tools = null,
        bool background = true,
        bool store = true,
        bool stream = false,
        string? previousInteractionId = null)
    {
        return new InteractionCreateRequest
        {
            Agent = agent,
            Input = input,
            AgentConfig = agentConfig ?? InteractionDeepResearchAgentConfig.Default,
            Tools = tools,
            Background = background,
            Store = store,
            Stream = stream ? true : null,
            PreviousInteractionId = previousInteractionId
        };
    }

    /// <summary>
    /// Creates a Deep Research agent request from plain text input.
    /// </summary>
    public static InteractionCreateRequest ForDeepResearch(
        string input,
        string agent = GoogleManagedAgentIds.DeepResearchPreview042026,
        InteractionDeepResearchAgentConfig? agentConfig = null,
        List<InteractionTool>? tools = null,
        bool background = true,
        bool store = true,
        bool stream = false,
        string? previousInteractionId = null) =>
        ForDeepResearch(InteractionInput.FromText(input), agent, agentConfig, tools, background, store, stream, previousInteractionId);

    /// <summary>
    /// Creates a Deep Research Max agent request.
    /// </summary>
    public static InteractionCreateRequest ForDeepResearchMax(
        string input,
        InteractionDeepResearchAgentConfig? agentConfig = null,
        List<InteractionTool>? tools = null,
        bool background = true,
        bool store = true,
        bool stream = false,
        string? previousInteractionId = null) =>
        ForDeepResearch(input, GoogleManagedAgentIds.DeepResearchMaxPreview042026, agentConfig, tools, background, store, stream, previousInteractionId);

    internal string Serialize() => VendorGoogleInteractionsJson.SerializeRequest(this);

    internal Dictionary<string, object?>? GetApiRevisionHeaders()
    {
        string? header = ApiRevision.ToHeaderValue();
        return header is null ? null : new Dictionary<string, object?> { ["Api-Revision"] = header };
    }
}
