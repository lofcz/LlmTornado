using LlmTornado.Agents.DataModels;
using LlmTornado.ChatFunctions;

namespace LlmTornado.Acp;

/// <summary>
/// Extension methods for converting ChatRuntime events to ACP session updates.
/// </summary>
public static partial class AcpTornadoExtension
{
    /// <summary>
    /// Converts a ChatRuntimeEvents instance to zero or more ACP session update notifications.
    /// Handles nested AgentRunnerEvents for tool calls and streaming.
    /// </summary>
    public static List<AcpSessionUpdate>? ToAcpSessionUpdates(this ChatRuntimeEvents evt)
    {
        return evt switch
        {
            ChatRuntimeAgentRunnerEvents agentRunnerEvt => ConvertAgentRunnerEvent(agentRunnerEvt.AgentRunnerEvent),
            ChatRuntimeCompletedEvent => null, // Completion is signalled by the prompt response, not a separate notification
            ChatRuntimeStartedEvent => null, // Start is implicit
            ChatRuntimeErrorEvent error =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = $"\n\n[Error: {error.Exception?.Message ?? "Unknown error"}]"
                    }
                }
            ],
            ChatRuntimeCancelledEvent =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = "\n\n[Operation cancelled]"
                    }
                }
            ],
            _ => null
        };
    }

    /// <summary>
    /// Converts an AgentRunnerEvents instance to ACP session updates.
    /// This is the core bridge that maps tool invocations, streaming tokens,
    /// and errors from the TornadoRunner agentic loop into ACP protocol notifications.
    /// </summary>
    private static List<AcpSessionUpdate>? ConvertAgentRunnerEvent(AgentRunnerEvents evt)
    {
        return evt switch
        {
            AgentRunnerStreamingEvent streamEvt => ConvertStreamingEvent(streamEvt),
            AgentRunnerToolInvokedEvent toolInvoked => ConvertToolInvokedEvent(toolInvoked),
            AgentRunnerToolCompletedEvent toolCompleted => ConvertToolCompletedEvent(toolCompleted),
            AgentRunnerErrorEvent errorEvt =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = $"\n\n[Error: {errorEvt.ErrorMessage}]"
                    }
                }
            ],
            AgentRunnerUsageReceivedEvent => null, // Usage tracking — no ACP notification needed
            AgentRunnerMaxTurnsReachedEvent =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = "\n\n[Max turns reached — stopping]"
                    }
                }
            ],
            AgentRunnerMaxTokensReachedEvent =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = "\n\n[Token limit reached — stopping]"
                    }
                }
            ],
            _ => null
        };
    }

    /// <summary>
    /// Converts streaming events (text deltas, reasoning) to ACP message chunks.
    /// </summary>
    private static List<AcpSessionUpdate>? ConvertStreamingEvent(AgentRunnerStreamingEvent streamEvt)
    {
        return streamEvt.ModelStreamingEvent switch
        {
            ModelStreamingOutputTextDeltaEvent textDelta when !string.IsNullOrEmpty(textDelta.DeltaText) =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = textDelta.DeltaText
                    }
                }
            ],
            ModelStreamingReasoningPartAddedEvent reasoning when !string.IsNullOrEmpty(reasoning.DeltaText) =>
            [
                new AcpSessionUpdate
                {
                    SessionUpdateType = AcpSessionUpdateTypes.AgentThoughtChunk,
                    Content = new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = reasoning.DeltaText
                    }
                }
            ],
            _ => null
        };
    }

    /// <summary>
    /// Converts a tool invocation start event to an ACP ToolCall notification.
    /// </summary>
    private static List<AcpSessionUpdate>? ConvertToolInvokedEvent(AgentRunnerToolInvokedEvent evt)
    {
        FunctionCall fn = evt.ToolCalled;
        string toolCallId = fn.ToolCall?.Id ?? Guid.NewGuid().ToString("N");

        return
        [
            new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.ToolCall,
                ToolCallId = toolCallId,
                Title = fn.Name,
                Kind = DetermineToolKind(fn.Name),
                Status = AcpToolCallStatuses.InProgress,
                RawInput = ParseJsonOrString(fn.Arguments),
                Locations = ExtractToolLocations(fn.Name, fn.Arguments)
            }
        ];
    }

    /// <summary>
    /// Converts a tool completion event to an ACP ToolCallUpdate notification.
    /// </summary>
    private static List<AcpSessionUpdate>? ConvertToolCompletedEvent(AgentRunnerToolCompletedEvent evt)
    {
        FunctionCall fn = evt.ToolCall;
        string toolCallId = fn.ToolCall?.Id ?? Guid.NewGuid().ToString("N");
        bool ok = fn.Result?.InvocationSucceeded ?? false;
        string resultContent = fn.Result?.Content ?? string.Empty;

        return
        [
            new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.ToolCallUpdate,
                ToolCallId = toolCallId,
                Title = fn.Name,
                Kind = DetermineToolKind(fn.Name),
                Status = ok ? AcpToolCallStatuses.Completed : AcpToolCallStatuses.Failed,
                RawOutput = ParseJsonOrString(resultContent),
                Locations = ExtractToolLocations(fn.Name, fn.Arguments),
                ToolCallContent =
                [
                    new AcpToolCallContent
                    {
                        Type = "content",
                        Content = new AcpContentBlock
                        {
                            Type = AcpContentBlockTypes.Text,
                            Text = Truncate(resultContent, 4000)
                        }
                    }
                ]
            }
        ];
    }

    /// <summary>
    /// Determines the ACP tool kind category based on the tool name.
    /// </summary>
    public static string DetermineToolKind(string toolName)
    {
        return toolName switch
        {
            "read_file" => AcpToolKinds.Read,
            "list_dir" => AcpToolKinds.Read,
            "search_files" => AcpToolKinds.Search,
            "write_file" => AcpToolKinds.Edit,
            "replace_in_file" => AcpToolKinds.Edit,
            _ => AcpToolKinds.Other
        };
    }

    /// <summary>
    /// Parses a JSON string, returning the deserialized object or the raw string if parsing fails.
    /// </summary>
    public static object? ParseJsonOrString(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(payload);
        }
        catch
        {
            return payload;
        }
    }

    /// <summary>
    /// Extracts file locations from tool call arguments for ACP location annotations.
    /// </summary>
    public static List<AcpToolCallLocation>? ExtractToolLocations(string toolName, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        try
        {
            using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(arguments);
            System.Text.Json.JsonElement root = doc.RootElement;

            string? relativePath = null;
            int? line = null;

            if (root.ValueKind is System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("relativePath", out System.Text.Json.JsonElement rp) && rp.ValueKind is System.Text.Json.JsonValueKind.String)
                {
                    relativePath = rp.GetString();
                }

                if (toolName == "read_file" && root.TryGetProperty("startLine", out System.Text.Json.JsonElement start) && start.TryGetInt32(out int startLine))
                {
                    line = startLine;
                }
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            return
            [
                new AcpToolCallLocation
                {
                    Path = relativePath,
                    Line = line
                }
            ];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Truncates a string to a maximum length, appending "..." if truncated.
    /// </summary>
    public static string Truncate(string s, int maxLen)
    {
        string oneLine = s.ReplaceLineEndings(" ");
        return oneLine.Length <= maxLen ? oneLine : string.Concat(oneLine.AsSpan(0, maxLen), "...");
    }

    // --- Legacy single-update conversion (kept for backward compatibility) ---

    /// <summary>
    /// Converts a ChatRuntimeEvents instance to a single ACP session update notification.
    /// Prefer <see cref="ToAcpSessionUpdates"/> which handles AgentRunner events properly.
    /// </summary>
    public static AcpSessionUpdate ToAcpSessionUpdate(this ChatRuntimeEvents evt)
    {
        return evt switch
        {
            ChatRuntimeCompletedEvent => new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock
                {
                    Type = AcpContentBlockTypes.Text,
                    Text = "Agent completed processing."
                }
            },
            ChatRuntimeErrorEvent error => new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock
                {
                    Type = AcpContentBlockTypes.Text,
                    Text = $"Error: {error.Exception?.Message ?? "Unknown error"}"
                }
            },
            ChatRuntimeCancelledEvent => new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock
                {
                    Type = AcpContentBlockTypes.Text,
                    Text = "Operation cancelled."
                }
            },
            _ => new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock
                {
                    Type = AcpContentBlockTypes.Text,
                    Text = $"Runtime event: {evt.EventType}"
                }
            }
        };
    }
}
