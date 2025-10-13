using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;

namespace ChatBot.States;

public class SimpleAgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    OrchestrationRuntimeConfiguration _runtime;

    public SimpleAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, ChatModel? model = null) : base(orchestrator)
    {
        _runtime = orchestrator;

        string instructions = $"""
                You are a helpful assistant.
                """;

        Agent = new TornadoAgent(
            client: client,
            model: model ?? ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Simple Agent",
            instructions: instructions,
            streaming: true
            );
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        List<ChatMessage> history = _runtime.GetMessages();

        Conversation conv = await Agent.RunAsync(
            appendMessages: history,
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return conv.Messages.Last();
    }
}