using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;

namespace ChatBot.States;

public struct RequiresPlanning
{
    public bool PlanningRequired { get; set; }
}

public class SelectorResult
{
    public bool RequiresPlanning { get; set; } = false;
    public ChatMessage Input { get; set; }
}


public class SelectorAgentRunnable : OrchestrationRunnable<ChatMessage, SelectorResult>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    OrchestrationRuntimeConfiguration _runtime;

    public SelectorAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator) : base(orchestrator)
    {
        _runtime = orchestrator;

        string instructions = $"""
                You are a helpful selector assistant. Based off the following conversation and user Task determine if a complex reasoning & planning is required to answer the users question.
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Selector Agent",
            instructions: instructions,
            outputSchema: typeof(RequiresPlanning)
            );

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<SelectorResult> Invoke(RunnableProcess<ChatMessage, SelectorResult> process)
    {
        List<ChatMessage> history = _runtime.GetMessages();
        history.Add(process.Input);

        Conversation conv = await Agent.RunAsync(
            appendMessages: history);

        conv.Messages.Last().Content.TryParseJson<RequiresPlanning>(out RequiresPlanning requires);

        return new SelectorResult
        {
            RequiresPlanning = requires.PlanningRequired,
            Input = process.Input
        };
    }
}