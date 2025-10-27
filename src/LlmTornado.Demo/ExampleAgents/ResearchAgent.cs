using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static LlmTornado.Demo.ExampleAgents.ResearchAgent.ResearchAgentConfiguration;

namespace LlmTornado.Demo.ExampleAgents.ResearchAgent;

#region Data Models
public struct WebSearchPlan
{
    public WebSearchItem[] items { get; set; }
    public WebSearchPlan(WebSearchItem[] items)
    {
        this.items = items;
    }
}

public struct WebSearchItem
{
    public string reason { get; set; }
    public string query { get; set; }

    public WebSearchItem(string reason, string query)
    {
        this.reason = reason;
        this.query = query;
    }
}

public struct ReportData
{
    public string ShortSummary { get; set; }
    public string FinalReport { get; set; }
    public string[] FollowUpQuestions { get; set; }
    public ReportData(string shortSummary, string finalReport, string[] followUpQuestions)
    {
        this.ShortSummary = shortSummary;
        this.FinalReport = finalReport;
        this.FollowUpQuestions = followUpQuestions;
    }

    public override string ToString()
    {
        return $@"
Summary: 
{ShortSummary}

Final Report: 
{FinalReport}


Follow Up Questions: 
{string.Join("\n", FollowUpQuestions)}
";
    }
}

#endregion

public class ResearchAgentConfiguration : OrchestrationRuntimeConfiguration
{
    PlannerRunnable planner;
    ResearchRunnable researcher;
    ReportingRunnable reporter;
    ExitRunnable exit;
    public int MaxDepth { get; set; } = 3;
    public int MinWordCount { get; set; } = 250;
    public TornadoApi Client { get; set; }

    public ResearchAgentConfiguration()
    {
        Client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;
        //Create the Runnables
        planner = new PlannerRunnable(Client, this, MaxDepth);
        researcher = new ResearchRunnable(Client, this, MaxDepth);
        reporter = new ReportingRunnable(Client, this, MinWordCount);
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        planner.AddAdvancer((plan) => !string.IsNullOrWhiteSpace(plan), researcher);
        researcher.AddAdvancer((research) => !string.IsNullOrEmpty(research), reporter);
        reporter.AddAdvancer((reporter) => !string.IsNullOrEmpty(reporter.FinalReport), exit);

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(planner);
        SetRunnableWithResult(exit);
    }

    public ResearchAgentConfiguration(TornadoApi? client = null)
    {
        Client = client ?? new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;
        //Create the Runnables
        planner = new PlannerRunnable(Client, this, MaxDepth);
        researcher = new ResearchRunnable(Client, this, MaxDepth);
        reporter = new ReportingRunnable(Client, this, MinWordCount);
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        planner.AddAdvancer((plan) => !string.IsNullOrWhiteSpace(plan), (result)=>new ChatMessage(ChatMessageRoles.Assistant,result), researcher);
        researcher.AddAdvancer((research) => !string.IsNullOrEmpty(research), reporter);
        reporter.AddAdvancer((reporter) => !string.IsNullOrEmpty(reporter.FinalReport), exit);

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(planner);
        SetRunnableWithResult(exit);
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();

        reporter.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };
    }
}


public class PlannerRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent;
    private int _maxItemsToPlan = 10;
    public PlannerRunnable(TornadoApi client, Orchestration orchestrator, int maxItemsToPlan) : base(orchestrator)
    {
        _maxItemsToPlan = maxItemsToPlan;
        string instructions = $"""
                You are a helpful research assistant. Given a query, come up with a set of web searches, 
                to perform to best answer the query. Output between 1 and {_maxItemsToPlan} terms to query for. 
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Research Agent",
            outputSchema: typeof(WebSearchPlan),
            instructions: instructions);
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(agent: Agent);

        Conversation conv = await Agent.Run(appendMessages: new List<ChatMessage> { process.Input });

       return conv.Messages.Last().Content ?? string.Empty;
    }
}

public class ResearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int MaxItemsToProcess { get; set; } = 3;
    TornadoApi Client { get; set; }
    public ResearchRunnable(TornadoApi client, Orchestration orchestrator, int maxItemsToProcess = 3) : base(orchestrator) { Client = client; MaxItemsToProcess = maxItemsToProcess; }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        WebSearchPlan? plan = await process.Input.Content?.SmartParseJsonAsync<WebSearchPlan>(new TornadoAgent(Client,ChatModel.OpenAi.Gpt41.V41Mini));

        SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);

        List<Task<string>> researchTasks =
            plan.Value.items
                .Take(MaxItemsToProcess)
                .Select(item => Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await RunResearchAgent(item.query);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }))
                .ToList();

        string[] researchResults = await Task.WhenAll(researchTasks);

        return string.Join("[RESEARCH RESULT]\n\n\n", researchResults.ToList().Select(result => result));
    }


    private async ValueTask<string> RunResearchAgent(string item)
    {
        string instructions = """
                You are a research assistant. Given a search term, you search the web for that term and
                produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                words. Capture the main points. Write succinctly, no need to have complete sentences or good
                grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                """;

        TornadoAgent Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Research Agent",
            instructions: instructions);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };

        ChatMessage userMessage = new ChatMessage(Code.ChatMessageRoles.User, item);

        Conversation conv = await Agent.Run(appendMessages: new List<ChatMessage> { userMessage });

        return conv.Messages.Last().Content ?? string.Empty;
    }

}

public class ReportingRunnable : OrchestrationRunnable<string, ReportData>
{
    TornadoAgent Agent;

    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }
    private int _minWordCount { get; set; } = 200;
    public ReportingRunnable(TornadoApi client, Orchestration orchestrator, int minWordCount) : base(orchestrator)
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
            model: ChatModel.OpenAi.Gpt5.V5,
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

public class ExitRunnable : OrchestrationRunnable<ReportData, ChatMessage>
{
    public ExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ReportData, ChatMessage> process)
    {
        this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
        return new ChatMessage(Code.ChatMessageRoles.Assistant, process.Input.ToString());
    }
}
