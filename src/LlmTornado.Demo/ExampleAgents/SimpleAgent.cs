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

namespace LlmTornado.Demo.ExampleAgents.SimpleAgent;

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

public class SimpleAgentConfiguration : OrchestrationRuntimeConfiguration
{
    AgentRunnable agent;

    ExitRunnable exit;
    public TornadoApi Client { get; set; }

    public SimpleAgentConfiguration(bool streaming = false)
    {
        Client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;

        //Create the Runnables
        agent = new AgentRunnable(Client, this, streaming);
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        agent.AddAdvancer(exit);


        //Configure the Orchestration entry and exit points
        SetEntryRunnable(agent);
        SetRunnableWithResult(exit);
    }

    public SimpleAgentConfiguration(TornadoApi? client = null, bool streaming = false)
    {
        Client = client ?? new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;
        //Create the Runnables
        agent = new AgentRunnable(Client, this, streaming);
        exit = new ExitRunnable(this) { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

        //Setup the orchestration flow
        agent.AddAdvancer(exit);

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(agent);
        SetRunnableWithResult(exit);
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();

        agent.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };
    }
}


public class AgentRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }


    public AgentRunnable(TornadoApi client, Orchestration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a research agent. You will be provided with a question to research.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Assistant",
            instructions: instructions,
            streaming:streaming);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(Agent);

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input }, 
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return conv.Messages.LastOrDefault(msg => msg.Content is not null)?.Content ?? "Content was null";
    }
}


public class ExitRunnable : OrchestrationRunnable<string, ChatMessage>
{
    public ExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<string, ChatMessage> process)
    {
        this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
        return new ChatMessage(Code.ChatMessageRoles.Assistant, process.Input);
    }
}
