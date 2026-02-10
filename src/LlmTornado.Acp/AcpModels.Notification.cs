using System.Text.Json.Serialization;

namespace LlmTornado.Acp;

/// <summary>
/// Session update notification from the agent.
/// </summary>
public class AcpSessionNotification
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("update")]
    public AcpSessionUpdate Update { get; set; } = new();
}

/// <summary>
/// Session update content.
/// </summary>
public class AcpSessionUpdate
{
    [JsonPropertyName("sessionUpdate")]
    public string SessionUpdateType { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public AcpContentBlock? Content { get; set; }

    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rawInput")]
    public object? RawInput { get; set; }

    [JsonPropertyName("rawOutput")]
    public object? RawOutput { get; set; }

    [JsonPropertyName("entries")]
    public List<AcpPlanEntry>? Entries { get; set; }

    [JsonPropertyName("currentModeId")]
    public string? CurrentModeId { get; set; }

    [JsonPropertyName("availableCommands")]
    public List<AcpAvailableCommand>? AvailableCommands { get; set; }

    [JsonPropertyName("locations")]
    public List<AcpToolCallLocation>? Locations { get; set; }

    [JsonPropertyName("toolCallContent")]
    public List<AcpToolCallContent>? ToolCallContent { get; set; }
}

/// <summary>
/// Session update type identifiers.
/// </summary>
public static class AcpSessionUpdateTypes
{
    public const string UserMessageChunk = "user_message_chunk";
    public const string AgentMessageChunk = "agent_message_chunk";
    public const string AgentThoughtChunk = "agent_thought_chunk";
    public const string ToolCall = "tool_call";
    public const string ToolCallUpdate = "tool_call_update";
    public const string Plan = "plan";
    public const string AvailableCommandsUpdate = "available_commands_update";
    public const string CurrentModeUpdate = "current_mode_update";
    public const string ConfigOptionUpdate = "config_option_update";
}

/// <summary>
/// A plan entry in the execution plan.
/// </summary>
public class AcpPlanEntry
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = AcpPlanEntryPriorities.Medium;

    [JsonPropertyName("status")]
    public string Status { get; set; } = AcpPlanEntryStatuses.Pending;
}

/// <summary>
/// Priority levels for plan entries.
/// </summary>
public static class AcpPlanEntryPriorities
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
}

/// <summary>
/// Status of a plan entry.
/// </summary>
public static class AcpPlanEntryStatuses
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
}

/// <summary>
/// Information about a command.
/// </summary>
public class AcpAvailableCommand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// A file location accessed or modified by a tool.
/// </summary>
public class AcpToolCallLocation
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int? Line { get; set; }
}

/// <summary>
/// Content produced by a tool call.
/// </summary>
public class AcpToolCallContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "content";

    [JsonPropertyName("content")]
    public AcpContentBlock? Content { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("oldText")]
    public string? OldText { get; set; }

    [JsonPropertyName("newText")]
    public string? NewText { get; set; }

    [JsonPropertyName("terminalId")]
    public string? TerminalId { get; set; }
}

/// <summary>
/// Tool call status values.
/// </summary>
public static class AcpToolCallStatuses
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

/// <summary>
/// Tool kind categories.
/// </summary>
public static class AcpToolKinds
{
    public const string Read = "read";
    public const string Edit = "edit";
    public const string Delete = "delete";
    public const string Move = "move";
    public const string Search = "search";
    public const string Execute = "execute";
    public const string Think = "think";
    public const string Fetch = "fetch";
    public const string SwitchMode = "switch_mode";
    public const string Other = "other";
}
