using System;
using System.Collections.Generic;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

public class AnthropicCacheSettings
{
    public static readonly AnthropicCacheSettings Ephemeral = new AnthropicCacheSettings();
    
    [JsonProperty("type")]
    public string Type { get; set; } = "ephemeral";

    private AnthropicCacheSettings()
    {
        
    }
}

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
}