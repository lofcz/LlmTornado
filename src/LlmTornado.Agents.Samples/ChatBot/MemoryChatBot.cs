using ChatBot.States;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples;

public class MemoryChatBot : OrchestrationRuntimeConfiguration
{
    PlannerRunnable planner;
    ResearchRunnable researcher;
    ReportingRunnable reporter;

    SelectorAgentRunnable selector;
    SimpleAgentRunnable simpleAgent;
    VectorDataSaverRunnable dataSaver;
    VectorDBAgentRunnable contextAgent;
    ParallelRunnable parallelPassthru;
    ExitRunnable exit;

    public int MaxDepth { get; set; } = 3;
    public int MinWordCount { get; set; } = 250;
    public TornadoApi Client { get; set; }

    public MemoryChatBot(TornadoApi? api = null, ChatModel? model = null)
    {
        if (api is not null)
        {
            Client = api;
        }
        else
            Client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;
        //Create the Runnables
        planner = new PlannerRunnable(Client, this, MaxDepth);
        researcher = new ResearchRunnable(Client, this, MaxDepth);
        reporter = new ReportingRunnable(Client, this, MinWordCount);
        selector = new SelectorAgentRunnable(Client, this);
        simpleAgent = new SimpleAgentRunnable(Client, this);
        dataSaver = new VectorDataSaverRunnable(Client, this);
        contextAgent = new VectorDBAgentRunnable(Client, this) { AllowDeadEnd = true };
        parallelPassthru = new ParallelRunnable(this) { AllowsParallelAdvances = true };
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        parallelPassthru.AddAdvancer(selector);
        parallelPassthru.AddAdvancer(contextAgent);

        selector.AddAdvancer<ChatMessage>((selection) => selection.RequiresPlanning, (conversion) => conversion.Input, planner);
        selector.AddAdvancer<ChatMessage>((selection) => !selection.RequiresPlanning, (conversion) => conversion.Input, simpleAgent);

        simpleAgent.AddAdvancer(dataSaver);

        planner.AddAdvancer((plan) => plan.items.Length > 0, researcher);
        researcher.AddAdvancer((research) => !string.IsNullOrEmpty(research), reporter);
        reporter.AddAdvancer(dataSaver);


        dataSaver.AddAdvancer(exit);

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(parallelPassthru);
        SetRunnableWithResult(simpleAgent);
        
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();

        simpleAgent.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };

        reporter.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };
    }
}
