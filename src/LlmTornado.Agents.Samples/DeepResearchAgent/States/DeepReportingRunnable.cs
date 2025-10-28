using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Samples.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Agents.Samples.ResearchAgent;

public class DeepReportingRunnable : OrchestrationRunnable<string, ReportData>
{
    TornadoAgent Agent;

    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }
    private int _minWordCount { get; set; } = 500;
    public DeepReportingRunnable(TornadoApi client, Orchestration orchestrator, int minWordCount) : base(orchestrator)
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
            model: ChatModel.OpenAi.Gpt5.V5Pro,
            name: "Report Agent",
            instructions: instructions,
            outputSchema: typeof(ReportData),
            streaming: true);

        _minWordCount = minWordCount;
    }

    public override async ValueTask<ReportData> Invoke(RunnableProcess<string, ReportData> research)
    {
        research.RegisterAgent(agent: Agent);

        Conversation conv = await Agent.Run(
            appendMessages: new List<ChatMessage> { new ChatMessage(Code.ChatMessageRoles.User, research.Input) },
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        ReportData? report = await conv.Messages.Last().Content?.SmartParseJsonAsync<ReportData>(Agent)!;

        if (report is null || string.IsNullOrWhiteSpace(report?.FinalReport))
        {
            throw new Exception("No report generated");
        }

        return report.Value;
    }
}
