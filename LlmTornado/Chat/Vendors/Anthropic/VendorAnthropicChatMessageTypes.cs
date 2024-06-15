using LlmTornado.Chat;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal class VendorAnthropicChatMessageTypes
{
    /// <summary>
    ///     A special identifier representing an intent to invoke a tool by the model.
    /// </summary>
    public const string ToolUse = "tool_use";
    
    /// <summary>
    ///     A special identifier used to return the resolved tool results back to the model along with tool_use_id and content.
    /// </summary>
    public const string ToolResult = "tool_result";
}