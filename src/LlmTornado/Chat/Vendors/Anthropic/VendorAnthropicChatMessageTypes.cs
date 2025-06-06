using System.Collections.Frozen;
using System.Collections.Generic;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal enum VendorAnthropicChatMessageTypes
{
    Unknown,
    Text,
    ToolUse,
    ToolResult,
    Thinking
}

internal static class VendorAnthropicChatMessageTypesCls
{
    public static readonly FrozenDictionary<string, VendorAnthropicChatMessageTypes> Map = new Dictionary<string, VendorAnthropicChatMessageTypes>
    {
        { "text", VendorAnthropicChatMessageTypes.Text },
        { "tool_use", VendorAnthropicChatMessageTypes.ToolUse },
        { "tool_result", VendorAnthropicChatMessageTypes.ToolResult },
        { "thinking", VendorAnthropicChatMessageTypes.Thinking }
    } .ToFrozenDictionary();
}