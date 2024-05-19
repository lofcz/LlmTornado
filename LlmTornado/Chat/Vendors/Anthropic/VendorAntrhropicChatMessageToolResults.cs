using System;
using System.Collections.Generic;

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
    public string Content { get; set; }
    
    /// <summary>
    ///     Type of the data from which <see cref="Content"/> was serialized.
    /// </summary>
    public Type? ContentType { get; set; }
    
    public string ToolCallId { get; set; }
    
    public bool? ToolInvocationSucceeded { get; set; }

    public VendorAnthropicChatMessageToolResult(ChatMessage msg)
    {
        Content = msg.Content;
        ContentType = msg.ContentJsonType;
        ToolCallId = msg.ToolCallId;
        ToolInvocationSucceeded = msg.ToolInvocationSucceeded;
    }
}