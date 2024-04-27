using LlmTornado.Chat;

namespace LlmTornado.Vendor.Anthropic;

internal class VendorAnthropicChatMessageTypes : ChatMessageTypes
{
    public VendorAnthropicChatMessageTypes(string? value) : base(value)
    {
    }

    /// <summary>
    ///     A special identifier representing an intent to invoke a tool by the model.
    /// </summary>
    public static VendorAnthropicChatMessageTypes ToolUse => new("tool_use");
    
    /// <summary>
    ///     A special identifier used to return the resolved tool results back to the model along with tool_use_id and content.
    /// </summary>
    public static VendorAnthropicChatMessageTypes ToolResult => new("tool_result");
}