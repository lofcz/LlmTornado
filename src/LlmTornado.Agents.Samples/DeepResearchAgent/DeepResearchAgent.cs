using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Code;

namespace LlmTornado.Agents.Samples.ResearchAgent;

public class DeepResearchAgentConfiguration : OrchestrationRuntimeConfiguration
{
    DeepPlannerRunnable planner;
    DeepResearchRunnable researcher;
    DeepReportingRunnable reporter;
    DeepExitRunnable exit;
    /// <summary>
    /// Max number of queries the agent can perform
    /// </summary>
    public int MaxQueries { get; set; } = 3;

    /// <summary>
    /// Minimum word count for the final report
    /// </summary>
    public int MinWordCount { get; set; } = 250;
    public TornadoApi Client { get; set; }

    public DeepResearchAgentConfiguration()
    {
        Client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;
        //Create the Runnables
        planner = new DeepPlannerRunnable(Client, this, MaxQueries);
        researcher = new DeepResearchRunnable(Client, this, MaxQueries);
        reporter = new DeepReportingRunnable(Client, this, MinWordCount);
        exit = new DeepExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        planner.AddAdvancer((plan) => plan.items.Length > 0, researcher);
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
