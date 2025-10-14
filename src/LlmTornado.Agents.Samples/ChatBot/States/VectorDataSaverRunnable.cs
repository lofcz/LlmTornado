using ChatBot.DataModels;
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
using System.Diagnostics;

namespace ChatBot.States;

public class VectorDataSaverRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;

    OrchestrationRuntimeConfiguration _runtime;
    TornadoPgVector pgVector;
    string connectionString = "Host=localhost;Database=postgres;Username=postgres;Password=john";

    public VectorDataSaverRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, ChatModel? model = null) : base(orchestrator)
    {
        _runtime = orchestrator;

        // Initialize with connection string and vector dimension
        pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

        string instructions = $"""
                You are a helpful assistant. Given the following Context and question generate a contextually compressed result (250 words max each) based on the context provided. 
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            name: "Vector Save Agent",
            instructions: instructions,
            outputSchema: typeof(CompressedTaskResult)
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

        string CurrentTask = _runtime.GetLastMessage().Content ?? _runtime.GetLastMessage().Parts.Where(part => !string.IsNullOrEmpty(part.Text)).Last().Text ?? "";

        if (checkContextOversized(CurrentTask) || checkContextOversized(process.Input.Content))
        {
            CompressedTaskResult compressed = await CompressContext(process.Input.Content, CurrentTask);
            await SaveResult(process.Input.Content, compressed.Result, CurrentTask, new Dictionary<string, object> { { "compressed", true } });
            await SaveTask(CurrentTask, compressed.Task, process.Input.Content, new Dictionary<string, object> { { "compressed", true } });
        }
        else
        {
            await SaveResult(process.Input.Content, process.Input.Content, CurrentTask);
            await SaveTask(CurrentTask, process.Input.Content, "completed");
        }

        return process.Input;
    }


    public async Task<CompressedTaskResult> CompressContext(string context, string task)
    {
        List<ChatMessage> history = _runtime.GetMessages();

        string compressionRequest = $"""
                Given the following task: {task}
                And the following result: {context}
                Generate a concise summary of the result that captures the main points. The summary must be 1-2 paragraphs and less than 250 words.
                Write succinctly, no need to have complete sentences or good grammar. This will be consumed by someone synthesizing a report, so its vital you capture the essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                """;

        Conversation conv = await Agent.RunAsync(
            input: compressionRequest,
            appendMessages: history
        );

        CompressedTaskResult? compressedResult = await conv.Messages.Last().Content?.SmartParseJsonAsync<CompressedTaskResult>(Agent);

        if (compressedResult.HasValue)
        {
            return compressedResult.Value;
        }
        else
        {
            throw new Exception("Failed to compress context");
        }
    }

    public bool checkContextOversized(string context)
    {
        if(context.Length >1500)
        {
            return true;
        }
        return false;
    }

    public async Task SaveTask(string content, string textToEmbed, string result, Dictionary<string, object>? meta = null)
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "type", "task" },
            { "timestamp", DateTime.UtcNow },
            { "result", result }    
        };

        if (meta != null)
        {
            foreach (var kvp in meta)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        await SaveDoc(content, textToEmbed, metadata);
    }

    public async Task SaveResult(string content, string textToEmbed, string task, Dictionary<string, object>? meta = null)
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "type", "result" },
            { "timestamp", DateTime.UtcNow },
            { "task", task }
        };
        if (meta != null)
        {
            foreach (var kvp in meta)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }
        await SaveDoc(content, textToEmbed, metadata);
    }

    public async Task SaveDoc(string content,string textToEmbed, Dictionary<string, object>? meta = null)
    {
        try
        {
            VectorDocument document = new VectorDocument(
                id: Guid.NewGuid().ToString(),
                content: content,
                embedding: Agent.Client.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, textToEmbed).Result.Data.FirstOrDefault()?.Embedding ?? throw new Exception("Failed to create embedding"),
                metadata: meta
            );

            await pgVector.AddDocumentsAsync([document]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to DB: {ex.Message}");
        }
    }
}