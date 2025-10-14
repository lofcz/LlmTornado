using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace ChatBot.States;

public class PlannerRunnable : OrchestrationRunnable<ChatMessage, WebSearchPlan>
{
    TornadoAgent Agent;
    private int _maxItemsToPlan = 10;
    public PlannerRunnable(TornadoApi client, Orchestration orchestrator, int maxItemsToPlan, ChatModel? model = null) : base(orchestrator)
    {
        _maxItemsToPlan = maxItemsToPlan;
        string instructions = $"""
                You are a helpful research assistant. Given a query, come up with a set of web searches, 
                to perform to best answer the query. Output between 1 and {_maxItemsToPlan} terms to query for. 
                """;

        Agent = new TornadoAgent(
            client: client,
            model: model ?? ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Research Agent",
            outputSchema: typeof(WebSearchPlan),
            instructions: instructions);
    }

    public override async ValueTask<WebSearchPlan> Invoke(RunnableProcess<ChatMessage, WebSearchPlan> process)
    {
        process.RegisterAgent(agent: Agent);

        string context = Orchestrator.RuntimeProperties.TryGetValue("LatestContext", out var ctx) ? ctx.ToString() ?? "Unavailable" : "Unavailable";

        process.Input.Content = $"""
                Context:
                {context}
                Question:
                {process.Input.Content}
                """;

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

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