using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace ChatBot.States;

public class ReportingRunnable : OrchestrationRunnable<string, ChatMessage>
{
    TornadoAgent Agent;

    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }
    private int _minWordCount { get; set; } = 200;
    public ReportingRunnable(TornadoApi client, Orchestration orchestrator, int minWordCount, ChatModel? model = null) : base(orchestrator)
    {
        string instructions = $"""
                You are a senior researcher tasked with writing a cohesive report for a research query.
                you will be provided with the original query, and some initial research done by a research assistant.

                you should first come up with an outline for the report that describes the structure and flow of the report. 
                Then, generate the report and return that as your final output.

                The final output should be in markdown format, and it should be lengthy and detailed. Aim for 1-2 pages of content, at least {_minWordCount} words.
                """;

        Agent = new TornadoAgent(
            client: client,
            model: model ?? ChatModel.OpenAi.Gpt5.V5,
            name: "Report Agent",
            instructions: instructions,
            streaming: true);

        _minWordCount = minWordCount;
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<string, ChatMessage> research)
    {
        research.RegisterAgent(agent: Agent);

        Conversation conv = await Agent.RunAsync(
            appendMessages: new List<ChatMessage> { new ChatMessage(ChatMessageRoles.User, research.Input) },
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return conv.Messages.Last();
    }
}
