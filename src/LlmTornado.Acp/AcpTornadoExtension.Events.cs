using LlmTornado.Agents.DataModels;

namespace LlmTornado.Acp;

/// <summary>
/// Extension methods for converting ChatRuntime events to ACP session updates.
/// </summary>
public static partial class AcpTornadoExtension
{
    /// <summary>
    /// Converts a ChatRuntimeEvents instance to an ACP session update notification.
    /// </summary>
    public static AcpSessionUpdate ToAcpSessionUpdate(this ChatRuntimeEvents evt)
    {
        return evt switch
        {
            ChatRuntimeCompletedEvent completed => new AcpSessionUpdate
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
            ChatRuntimeInvokedEvent invoked => new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = invoked.Message is not null
                    ? new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = invoked.Message.Content ?? string.Empty
                    }
                    : new AcpContentBlock
                    {
                        Type = AcpContentBlockTypes.Text,
                        Text = string.Empty
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
