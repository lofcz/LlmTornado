using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;

namespace ChatBot.States;

public class ResearchRunnable : OrchestrationRunnable<WebSearchPlan, string>
{
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int MaxItemsToProcess { get; set; } = 3;
    TornadoApi Client { get; set; }
    public ResearchRunnable(TornadoApi client, Orchestration orchestrator, int maxItemsToProcess = 3) : base(orchestrator) { Client = client; MaxItemsToProcess = maxItemsToProcess; }

    public override async ValueTask<string> Invoke(RunnableProcess<WebSearchPlan, string> process)
    {

        SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);

        List<Task<string>> researchTasks =
            process.Input.items
                .Take(MaxItemsToProcess)
                .Select(item => Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await RunResearchAgent(item, process);
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


    private async ValueTask<string> RunResearchAgent(WebSearchItem item, RunnableProcess<WebSearchPlan, string> process)
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

        process.RegisterAgent(Agent);

        ChatMessage userMessage = new ChatMessage(LlmTornado.Code.ChatMessageRoles.User, item.query);

        Conversation conv = await Agent.Run(appendMessages: new List<ChatMessage> { userMessage });

        return conv.Messages.Last().Content ?? string.Empty;
    }

}
