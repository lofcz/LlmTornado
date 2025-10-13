using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Responses;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.PgVector.Integrations;

namespace ChatBot.States;

public class VectorDBAgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    OrchestrationRuntimeConfiguration _runtime;
    TornadoPgVector pgVector;
    string connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=john";

    public VectorDBAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator) : base(orchestrator)
    {
        _runtime = orchestrator;

        // Initialize with connection string and vector dimension
        pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

        string instructions = $"""
                You are a helpful assistant. Given the following Context and question generate a contextually compressed result (250 words max) based on the context provided. 
                If the information is not available, or irrelevant, say "No Additional Context Available".
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            name: "Vector Agent",
            instructions: instructions
            );


        Task.Run(async () => await SetupDB()).Wait();
    }

    public async Task SetupDB()
    {
        // Initialize a collection
        await pgVector.InitializeCollection("agent_memory");
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        List<ChatMessage> history = _runtime.GetMessages();

        //Not handling images at this time.
        string context = await GenerateContext(process.Input.Content, 5);

        string prompt = $"""
                Context:
                {context}
                Question:
                {process.Input.Content}
                """;   
        
        Conversation conv = await Agent.RunAsync(
            input: prompt,
            appendMessages: history
        );

        _runtime.RuntimeProperties.AddOrUpdate("LatestContext", conv.Messages.Last().Content ?? "Unavailable", (key, oldValue) => conv.Messages.Last().Content ?? "Unavailable");

        return conv.Messages.Last();
    }

    public async Task<string> GenerateContext(string query, int topK)
    {
        VectorDocument[] results = await queryDb(query, topK);
        if (results.Length == 0)
        {
            return "No relevant context found.";
        }
        return string.Join("\n\n", results.Select(r => r.Content));
    }

    public async Task<VectorDocument[]> queryDb(string query, int topK)
    {
        VectorDocument[] results;
        try
        {
            float[] queryEmbedding = Agent.Client.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, query).Result.Data.FirstOrDefault()?.Embedding ?? throw new Exception("Failed to create embedding");

            results = await pgVector.QueryByEmbeddingAsync(
              embedding: queryEmbedding,
              topK: topK
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying DB: {ex.Message}");
            return Array.Empty<VectorDocument>();
        }

        return results;
    }
}