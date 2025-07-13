using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal class VendorAnthropicChatMessageToolResults
{
    public List<VendorAnthropicChatMessageToolResult> ToolResults = [];
}

internal class VendorAnthropicChatMessageToolResult
{
    /// <summary>
    ///     JSON result of the tool invocation.
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    ///     Type of the data from which <see cref="Content"/> was serialized.
    /// </summary>
    public Type? ContentType { get; set; }
    
    public string ToolCallId { get; set; }
    
    public bool? ToolInvocationSucceeded { get; set; }
    
    [JsonIgnore]
    public ChatMessage SourceMessage { get; set; }

    public VendorAnthropicChatMessageToolResult(ChatMessage msg)
    {
        Content = msg.Content;
        ContentType = msg.ContentJsonType;
        ToolCallId = msg.ToolCallId ?? string.Empty;
        ToolInvocationSucceeded = msg.ToolInvocationSucceeded;
        SourceMessage = msg;
    }
}