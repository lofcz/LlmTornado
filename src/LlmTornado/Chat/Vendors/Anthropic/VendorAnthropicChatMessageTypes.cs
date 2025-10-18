using System.Collections.Frozen;
using System.Collections.Generic;

namespace LlmTornado.Chat.Vendors.Anthropic;

internal enum VendorAnthropicChatMessageTypes
{
    Unknown,
    Text,
    ToolUse,
    ToolResult,
    Thinking,
    RedactedThinking,
    ServerToolUse,
    TextEditorCodeExecutionToolResult,
    WebSearchToolResult,
    WebFetchToolResult,
    CodeExecutionToolResult,
    BashCodeExecutionToolResult,
    McpToolUse,
    McpToolResult,
    ContainerUpload
}

internal static class VendorAnthropicChatMessageTypesCls
{
    public static readonly FrozenDictionary<string, VendorAnthropicChatMessageTypes> Map = new Dictionary<string, VendorAnthropicChatMessageTypes>
    {
        { "text", VendorAnthropicChatMessageTypes.Text },
        { "tool_use", VendorAnthropicChatMessageTypes.ToolUse },
        { "tool_result", VendorAnthropicChatMessageTypes.ToolResult },
        { "thinking", VendorAnthropicChatMessageTypes.Thinking },
        { "redacted_thinking", VendorAnthropicChatMessageTypes.RedactedThinking },
        {"server_tool_use", VendorAnthropicChatMessageTypes.ServerToolUse },
        {"text_editor_code_execution_tool_result", VendorAnthropicChatMessageTypes.TextEditorCodeExecutionToolResult },
        {"web_search_tool_result", VendorAnthropicChatMessageTypes.WebSearchToolResult },
        {"web_fetch_tool_result", VendorAnthropicChatMessageTypes.WebFetchToolResult },
        {"code_execution_tool_result", VendorAnthropicChatMessageTypes.CodeExecutionToolResult },
        {"bash_code_execution_tool_result", VendorAnthropicChatMessageTypes.BashCodeExecutionToolResult },
        {"mcp_tool_use", VendorAnthropicChatMessageTypes.McpToolUse },
        {"mcp_tool_result", VendorAnthropicChatMessageTypes.McpToolResult },
        {"container_upload", VendorAnthropicChatMessageTypes.ContainerUpload }

    } .ToFrozenDictionary();
}