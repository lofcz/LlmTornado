using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.Samples.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;

namespace LlmTornado.Agents.Samples.ResearchAgent;

public class DeepPlannerRunnable : OrchestrationRunnable<ChatMessage, WebSearchPlan>
{
    TornadoAgent Agent;
    private int _maxItemsToPlan = 10;
    public DeepPlannerRunnable(TornadoApi client, Orchestration orchestrator, int maxItemsToPlan) : base(orchestrator)
    {
        _maxItemsToPlan = maxItemsToPlan;
        string instructions = $"""
                You are a helpful research planner assistant. Given a query First research the topic briefly to understand the context. 
                Then break down the topic into {_maxItemsToPlan} specific search terms that will help gather relevant information about the topic.
                Output between 1 and {_maxItemsToPlan} terms to query for. 
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5,
            name: "Research Planner",
            outputSchema: typeof(WebSearchPlan),
            instructions: instructions);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<WebSearchPlan> Invoke(RunnableProcess<ChatMessage, WebSearchPlan> process)
    {
        process.RegisterAgent(agent: Agent);

        Conversation conv = await Agent.Run(appendMessages: new List<ChatMessage> { process.Input });

        WebSearchPlan? plan = await conv.Messages.Last().Parts?.LastOrDefault().Text.SmartParseJsonAsync<WebSearchPlan>(Agent);

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
