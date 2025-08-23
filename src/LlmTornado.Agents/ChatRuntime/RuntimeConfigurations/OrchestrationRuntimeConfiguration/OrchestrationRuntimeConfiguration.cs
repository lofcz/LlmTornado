using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Orchestration;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations.OrchestrationRuntimeConfiguration;

public class OrchestrationAgent : RuntimeAgent
{
    public OrchestrationAgent(TornadoApi client,
        Chat.Models.ChatModel model,
        string name = "Handoff Agent",
        string instructions = "You are a helpful assistant",
        Type? outputSchema = null,
        List<Delegate>? tools = null,
        List<MCPServer>? mcpServers = null,
        bool streaming = false) : base(client, model, name, instructions, outputSchema, tools, mcpServers, streaming)
    {

    }
}

public class OrchestrationRuntimeConfiguration : AgentOrchestration, IRuntimeConfiguration
{
    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
    public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }

    public OrchestrationRuntimeConfiguration()
    {
        OnOrchestrationEvent += (e) =>
        {
            OnRuntimeEvent?.Invoke(new ChatRuntimeOrchestrationEvent(e));
        };
    }

    public virtual async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        MessageHistory.Push(message);

        await InvokeAsync(message);

        return Results?.Last() ?? new ChatMessage(Code.ChatMessageRoles.Assistant, "Some sort of error");
    }

    public virtual void ClearMessages()
    {
        MessageHistory.Clear();
    }

    public virtual List<ChatMessage> GetMessages()
    {
        return MessageHistory.ToList();
    }

    public virtual ChatMessage GetLastMessage()
    {
        return MessageHistory.TryPeek(out var lastMessage) ? lastMessage : new ChatMessage();
    }
}
