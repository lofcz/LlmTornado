using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Demo.ExampleAgents;

public class ResearchAgentConfiguration : OrchestrationRuntimeConfiguration
{
    PlannerRunnable planner;
    ResearchRunnable researcher;
    ReportingRunnable reporter;
    ExitRunnable exit;

    public ResearchAgentConfiguration(TornadoApi client)
    {
        //Create the Runnables
        planner = new PlannerRunnable(client, this);
        researcher = new ResearchRunnable(client, this);
        reporter = new ReportingRunnable(client, this);
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        planner.AddAdvancer((plan) => plan.items.Length > 0, researcher);
        researcher.AddAdvancer((research) => !string.IsNullOrEmpty(research), reporter);
        reporter.AddAdvancer((report) => !string.IsNullOrEmpty(report.FinalReport), exit);

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(planner);
        SetRunnableWithResult(exit);  
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();
        planner.Orchestrator = this;
        researcher.Orchestrator = this;
        reporter.Orchestrator = this;
        exit.Orchestrator = this;

        OnOrchestrationEvent += (e) =>
        {
            // Forward orchestration events to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeOrchestrationEvent(e, Runtime?.Id ?? string.Empty));
        };

        reporter.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };
    }

    public class PlannerRunnable : OrchestrationRunnable<ChatMessage, WebSearchPlan>
    {
        RuntimeAgent Agent;

        public PlannerRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
        {
            string instructions = """
                You are a helpful research assistant. Given a query, come up with a set of web searches, 
                to perform to best answer the query. Output between 5 and 10 terms to query for. 
                """;

            Agent = new RuntimeAgent(
                client: client,
                model: ChatModel.OpenAi.Gpt5.V5Mini,
                name: "Research Agent",
                outputSchema: typeof(WebSearchPlan),
                instructions: instructions);

            RegisterAgentMetrics(Agent);
        }

        public override async ValueTask<WebSearchPlan> Invoke(ChatMessage input)
        {
            Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { input });

            WebSearchPlan? plan = await conv.Messages.Last().Content?.SmartParseJsonAsync<WebSearchPlan>(Agent);

            if (plan is null || plan?.items is null || plan?.items.Length == 0)
            {
                return new WebSearchPlan([]);
            }
            else
            {
                return plan.Value;
            }
        }
    }

    public class ResearchRunnable : OrchestrationRunnable<WebSearchPlan, string>
    {
        public int MaxDegreeOfParallelism { get; set; } = 4;
        public int MaxItemsToProcess { get; set; } = 3;
        TornadoApi Client { get; set; }
        public ResearchRunnable(TornadoApi client, Orchestration orchestrator):base(orchestrator) { Client = client; }

        public override async ValueTask<string> Invoke(WebSearchPlan plan)
        {
            return await InvokeThreadedAsync(plan);
        }

        public async Task<string> InvokeThreadedAsync(WebSearchPlan plan)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);

            List<Task<string>> researchTasks =
                plan.items
                    .Take(MaxItemsToProcess)
                    .Select(item => Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            return await RunResearchAgent(item);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }))
                    .ToList();

            var researchResults = await Task.WhenAll(researchTasks);

            return string.Join("[RESEARCH RESULT]\n\n\n", researchResults.ToList().Select(result => result));
        }

        private async ValueTask<string> RunResearchAgent(WebSearchItem item)
        {
            string instructions = """
                You are a research assistant. Given a search term, you search the web for that term and
                produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                words. Capture the main points. Write succinctly, no need to have complete sentences or good
                grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                """;

            RuntimeAgent Agent = new RuntimeAgent(
                client: Client,
                model: ChatModel.OpenAi.Gpt5.V5Mini,
                name: "Research Agent",
                instructions: instructions);

            Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };

            RegisterAgentMetrics(Agent);

            ChatMessage userMessage = new ChatMessage(Code.ChatMessageRoles.User, item.query);

            Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { userMessage });

            return conv.Messages.Last().Content ?? string.Empty;
        }
    }

    public class ReportingRunnable : OrchestrationRunnable<string, ReportData>
    {
        RuntimeAgent Agent;

        public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

        public ReportingRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
        {
            string instructions = """
                You are a senior researcher tasked with writing a cohesive report for a research query.
                you will be provided with the original query, and some initial research done by a research assistant.

                you should first come up with an outline for the report that describes the structure and flow of the report. 
                Then, generate the report and return that as your final output.

                The final output should be in markdown format, and it should be lengthy and detailed. Aim for 5-10 pages of content, at least 1000 words.
                """;

            Agent = new RuntimeAgent(
                client: client,
                model: ChatModel.OpenAi.Gpt5.V5,
                name: "Report Agent",
                instructions: instructions,
                outputSchema: typeof(ReportData),
                streaming: true);

            RegisterAgentMetrics(Agent);
        }

        public override async ValueTask<ReportData> Invoke(string research)
        {
            Conversation conv = await Agent.RunAsync(
                appendMessages: new List<ChatMessage> { new ChatMessage(Code.ChatMessageRoles.User, research) },
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

        public override ValueTask<ChatMessage> Invoke(ReportData input)
        {
            this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
            return ValueTask.FromResult(new ChatMessage(Code.ChatMessageRoles.Assistant, input.ToString()));
        }
    }

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
}
