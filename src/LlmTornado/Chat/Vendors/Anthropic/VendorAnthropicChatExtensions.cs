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
    public static AnthropicCacheSettings EphemeralWithTtl(AnthropicCacheTtlOptions ttl)
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
    public AnthropicCacheTtlOptions? Ttl { get; set; }

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
/// Time to live cache optins.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AnthropicCacheTtlOptions
{
    /// <summary>
    /// 5m
    /// </summary>
    [EnumMember(Value = "5m")]
    FiveMinutes,
    /// <summary>
    /// 1h
    /// </summary>
    [EnumMember(Value = "1h")]
    OneHour
}

/// <summary>
/// Thinking settings for Claude 3.7+ models.
/// </summary>
public class AnthropicThinkingSettings
{
    /// <summary>
    /// The budget_tokens parameter determines the maximum number of tokens Claude is allowed use for its internal reasoning process. Larger budgets can improve response quality by enabling more thorough analysis for complex problems, although Claude may not use the entire budget allocated, especially at ranges above 32K.
    /// <br/><b>Note: budget_tokens must always be less than the max_tokens specified.</b>
    /// </summary>
    public int? BudgetTokens { get; set; }
    
    /// <summary>
    /// Whether thinking is enabled
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Anthropic chat request item.
/// </summary>
public interface IAnthropicChatRequestItem
{
    
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
}