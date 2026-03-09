using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Cache settings used by Anthropic.
/// </summary>
public class AnthropicCacheSettings
{
    /// <summary>
    /// "ephemeral" type of cache, shared object.
    /// </summary>
    public static readonly AnthropicCacheSettings Ephemeral = new AnthropicCacheSettings();

    /// <summary>
    /// "ephemeral" type of cache, with variable time to live.
    /// </summary>
    public static AnthropicCacheSettings EphemeralWithTtl(ChatRequestCacheTtl ttl)
    {
        return new AnthropicCacheSettings
        {
            Type = AnthropicCacheTypes.Ephemeral,
            Ttl = ttl
        };
    }

    /// <summary>
    /// Cache type.
    /// </summary>
    [JsonProperty("type")]
    public AnthropicCacheTypes Type { get; set; } = AnthropicCacheTypes.Ephemeral;

    /// <summary>
    /// Time to live. Increasing this increases the price multiplier.
    /// </summary>
    [JsonProperty("ttl")]
    public ChatRequestCacheTtl? Ttl { get; set; }

    private AnthropicCacheSettings()
    {
        
    }
}

/// <summary>
/// Anthropic cache types.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicCacheTypes
{
    /// <summary>
    /// Ephemeral cache.
    /// </summary>
    [EnumMember(Value = "ephemeral")]
    Ephemeral
}


/// <summary>
/// Thinking settings for Claude 3.7+ models.
/// </summary>
public class AnthropicThinkingSettings
{
    /// <summary>
    /// The budget_tokens parameter determines the maximum number of tokens Claude is allowed use for its internal reasoning process. Larger budgets can improve response quality by enabling more thorough analysis for complex problems, although Claude may not use the entire budget allocated, especially at ranges above 32K.
    /// <br/><b>Note: budget_tokens must always be less than the max_tokens specified.</b>
    /// <br/><b>Deprecated on Claude 4.6+:</b> Use <see cref="Adaptive"/> with the effort parameter instead.
    /// </summary>
    public int? BudgetTokens { get; set; }
    
    /// <summary>
    /// Whether thinking is enabled with manual budget control (type: "enabled").
    /// <br/><b>Deprecated on Claude 4.6+:</b> Use <see cref="Adaptive"/> instead.
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Whether adaptive thinking is enabled (type: "adaptive"). Recommended for Claude 4.6+.
    /// Claude dynamically decides when and how much to think. Use the effort parameter to control thinking depth.
    /// When set to true, <see cref="BudgetTokens"/> and <see cref="Enabled"/> are ignored.
    /// </summary>
    public bool Adaptive { get; set; }
}

/// <summary>
/// Anthropic chat request item.
/// </summary>
public interface IAnthropicChatRequestItem
{
    
}

/// <summary>
/// Represents a skill that can be loaded in the container.
/// </summary>
public class AnthropicSkill
{
    /// <summary>
    /// Type of the skill (typically "anthropic" for built-in skills).
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "anthropic";
    
    /// <summary>
    /// Skill identifier (e.g., "xlsx", "pptx", "pdf").
    /// </summary>
    [JsonProperty("skill_id")]
    public string SkillId { get; set; }
    
    /// <summary>
    /// Version of the skill (typically "latest" or a specific version ID).
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; } = "latest";
    
    /// <summary>
    /// Creates a new Anthropic skill.
    /// </summary>
    /// <param name="skillId">Skill identifier (e.g., "xlsx", "pptx", "pdf")</param>
    /// <param name="version">Version of the skill (default: "latest")</param>
    public AnthropicSkill(string skillId, string version = "latest")
    {
        SkillId = skillId;
        Version = version;
        if(skillId == "xlsx" || skillId == "pptx" || skillId == "pdf" || skillId == "docx")
        {
            Type = "anthropic";
        }
        else
        {
            Type = "custom";
        }
    }
}

/// <summary>
/// Container configuration for loading skills and resources.
/// </summary>
public class AnthropicContainer
{
    /// <summary>
    /// List of skills to load in the container max 8
    /// </summary>
    [JsonProperty("skills")]
    public List<AnthropicSkill>? Skills { get; set; }
}

/// <summary>
/// MCP servers to be utilized in this request max 20
/// </summary>
public class AnthropicMcpServer
{
    /// <summary>
    /// Type of the skill (typically "anthropic" for built-in skills).
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; } = "url";

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("authorization_token")]
    public string? AuthorizationToken { get; set; }

    [JsonProperty("tool_configuration")]
    public AnthropicMcpConfiguration? Configuration { get; set; }
}

public class AnthropicMcpConfiguration
{
    [JsonProperty("allowed_tools")]
    public string[]? AllowedTools { get; set; }


    [JsonProperty("enabled")]
    public bool? Enabled { get; set; } = true;
}

/// <summary>
/// Supported trigger types for compaction.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicCompactionTriggerTypes
{
    /// <summary>
    /// Trigger compaction based on input token count.
    /// </summary>
    [EnumMember(Value = "input_tokens")]
    InputTokens
}

/// <summary>
/// Supported compaction edit types.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicCompactionEditTypes
{
    /// <summary>
    /// Compact context using server-side summarization (version 2026-01-12).
    /// </summary>
    [EnumMember(Value = "compact_20260112")]
    Compact20260112
}

/// <summary>
/// Supported inference geographic regions for data residency controls.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicInferenceGeoOptions
{
    /// <summary>
    /// Global routing (default). Inference may run in any available geography for optimal performance and availability.
    /// </summary>
    [EnumMember(Value = "global")]
    Global,
    
    /// <summary>
    /// US-only inference. Priced at 1.1x on Claude Opus 4.6 and newer models.
    /// </summary>
    [EnumMember(Value = "us")]
    Us
}

/// <summary>
/// Trigger configuration for when compaction should activate.
/// </summary>
public class AnthropicCompactionTrigger
{
    /// <summary>
    /// The type of trigger. Currently only <see cref="AnthropicCompactionTriggerTypes.InputTokens"/> is supported.
    /// </summary>
    [JsonProperty("type")]
    public AnthropicCompactionTriggerTypes Type { get; set; } = AnthropicCompactionTriggerTypes.InputTokens;
    
    /// <summary>
    /// The token count threshold at which compaction triggers. Must be at least 50,000.
    /// Default is 150,000 tokens.
    /// </summary>
    [JsonProperty("value")]
    public int Value { get; set; } = 150_000;
    
    /// <summary>
    /// Creates a trigger based on input token count.
    /// </summary>
    /// <param name="tokenThreshold">Token count threshold (minimum 50,000).</param>
    public AnthropicCompactionTrigger(int tokenThreshold)
    {
        Value = tokenThreshold;
    }
    
    /// <summary>
    /// Creates a trigger with default threshold (150,000 tokens).
    /// </summary>
    public AnthropicCompactionTrigger()
    {
    }
}

/// <summary>
/// A compaction edit entry for automatic context summarization.
/// </summary>
public class AnthropicCompactionEdit
{
    /// <summary>
    /// The type of edit. Currently only <see cref="AnthropicCompactionEditTypes.Compact20260112"/> is supported.
    /// </summary>
    [JsonProperty("type")]
    public AnthropicCompactionEditTypes Type { get; set; } = AnthropicCompactionEditTypes.Compact20260112;
    
    /// <summary>
    /// When to trigger compaction. Defaults to 150,000 tokens if not set.
    /// </summary>
    [JsonProperty("trigger")]
    public AnthropicCompactionTrigger? Trigger { get; set; }
    
    /// <summary>
    /// Whether to pause after generating the compaction summary, returning a response with the "compaction" stop reason.
    /// This allows you to add additional content blocks before the API continues.
    /// </summary>
    [JsonProperty("pause_after_compaction")]
    public bool? PauseAfterCompaction { get; set; }
    
    /// <summary>
    /// Custom summarization instructions. Completely replaces the default summarization prompt when provided.
    /// </summary>
    [JsonProperty("instructions")]
    public string? Instructions { get; set; }
}

/// <summary>
/// Context management configuration for server-side compaction.
/// Enables effectively infinite conversations by automatically summarizing older context.
/// </summary>
public class AnthropicContextManagement
{
    /// <summary>
    /// List of context management edits to apply.
    /// </summary>
    [JsonProperty("edits")]
    public List<AnthropicCompactionEdit> Edits { get; set; } = [];
    
    /// <summary>
    /// Creates a context management configuration with a default compaction edit.
    /// </summary>
    public static AnthropicContextManagement Default()
    {
        return new AnthropicContextManagement
        {
            Edits = [new AnthropicCompactionEdit()]
        };
    }
    
    /// <summary>
    /// Creates a context management configuration with a custom token threshold trigger.
    /// </summary>
    /// <param name="tokenThreshold">Token count threshold for triggering compaction (minimum 50,000).</param>
    public static AnthropicContextManagement WithTrigger(int tokenThreshold)
    {
        return new AnthropicContextManagement
        {
            Edits = [new AnthropicCompactionEdit { Trigger = new AnthropicCompactionTrigger(tokenThreshold) }]
        };
    }
}

/// <summary>
///     Chat features supported only by Anthropic.
/// </summary>
public class ChatRequestVendorAnthropicExtensions
{
    /// <summary>
    ///     Enables modification of the outbound chat request just before sending it. Use this to control cache in chat-like scenarios.<br/>
    ///     Arguments: <b>System message</b>; <b>User, Assistant messages</b>; <b>Tools</b>
    /// </summary>
    public Action<VendorAnthropicChatRequestMessageContent?, List<VendorAnthropicChatRequestMessageContent>, List<VendorAnthropicToolFunction>?>? OutboundRequest;
    
    /// <summary>
    /// Thinking settings for Claude 3.7+ models.<br/>
    /// Important: while supported, please use <see cref="ChatRequest.ReasoningBudget"/> instead.
    /// </summary>
    public AnthropicThinkingSettings? Thinking { get; set; }
    
    /// <summary>
    /// Server-side tools.
    /// </summary>
    public List<IVendorAnthropicChatRequestBuiltInTool>? BuiltInTools { get; set; }
    
    /// <summary>
    /// Container configuration for loading skills. Skills allow Claude to perform specialized tasks like creating PowerPoint presentations, Excel spreadsheets, or PDF documents.<br/>
    /// <b>Note:</b> When using skills, you must also include code execution in your tools and use the beta Messages API.
    /// </summary>
    public AnthropicContainer? Container { get; set; }

    /// <summary>
    /// List of MCP servers to be utilized in this request. (Max length 20)
    /// </summary>
    public List<AnthropicMcpServer>? McpServers { get; set; }
    
    /// <summary>
    /// Server-side context management configuration for automatic compaction.
    /// Enables effectively infinite conversations by summarizing older context when approaching window limits.
    /// Requires the compact-2026-01-12 beta header (added automatically).
    /// Available on Claude Opus 4.6 only.
    /// </summary>
    public AnthropicContextManagement? ContextManagement { get; set; }
    
    /// <summary>
    /// Controls where model inference runs for this request.
    /// Available on Claude Opus 4.6 and newer models only.
    /// </summary>
    public AnthropicInferenceGeoOptions? InferenceGeo { get; set; }
}