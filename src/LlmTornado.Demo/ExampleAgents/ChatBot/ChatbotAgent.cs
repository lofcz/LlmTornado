using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.VectorDatabases.Intergrations;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Demo.ExampleAgents.CSCodingAgent;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Moderation;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Demo.ExampleAgents.ChatBot;

public class ChatbotAgent 
{
    public OrchestrationRuntimeConfiguration Configuration { get; set; } = new OrchestrationRuntimeConfiguration();

    AgentRunnable RunnableAgent { get; set; }
    ModeratorRunnable inputModerator { get; set; }
    VectorSearchRunnable vectorSearchRunnable { get; set; }
    WebSearchRunnable webSearchRunnable { get; set; }   
    VectorSaveRunnable vectorSaveRunnable { get; set; }
    ExitPathRunnable exitPathRunnable { get; set; }
    public ChatbotAgent(TornadoApi client, bool streaming = false)
    {
        RunnableAgent = new AgentRunnable(client, Configuration, streaming);
        inputModerator = new ModeratorRunnable(client, Configuration);
        vectorSearchRunnable = new VectorSearchRunnable(client, Configuration, "http://localhost:8001/api/v2/");
        webSearchRunnable = new WebSearchRunnable(client, Configuration);
        vectorSaveRunnable = new VectorSaveRunnable(client, Configuration, "http://localhost:8001/api/v2/");
        exitPathRunnable = new ExitPathRunnable(Configuration);

        Configuration = new OrchestrationBuilder()
            .SetEntryRunnable(inputModerator)
            .SetOutputRunnable(RunnableAgent)
            .WithRuntimeProperty("LatestUserMessage", "")
            .AddParallelAdvancement(inputModerator,
                 new OrchestrationAdvancer<ChatMessage>(webSearchRunnable),
                 new OrchestrationAdvancer<ChatMessage>(vectorSearchRunnable))
            .AddCombinationalAdvancement<string>(
                fromRunnables: [webSearchRunnable, vectorSearchRunnable],
                condition: _ => true,
                toRunnable: RunnableAgent,
                requiredInputToAdvance: 1,
                combinationRunnableName: "CombinationalContextWaiter")
            .AddParallelAdvancement(RunnableAgent, 
                new OrchestrationAdvancer<ChatMessage>(vectorSaveRunnable),
                new OrchestrationAdvancer<ChatMessage>(exitPathRunnable))
            .AddExitPath<ChatMessage>(exitPathRunnable, _ => true)
            .CreateDotGraphVisualization("ChatBotAgent.dot")
            .Build();
           Console.WriteLine("ChatBotAgent Orchestration Created");
    }
}



public class ModeratorRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoApi Client { get; set; }
    public ModeratorRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        Client = client;
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> input)
    {
        await ThrowOnModeratedInput(input.Input, Client);
        return input.Input;
    }

    private async Task ThrowOnModeratedInput(ChatMessage Input, TornadoApi Client)
    {
        // Moderate input content by OpenAI Moderation API Standards
        if (Input.Content is not null)
        {
            ModerationResult modResult = await Client.Moderation.CreateModeration(Input.Content);
            if (modResult.Results.FirstOrDefault()?.Flagged == true)
            {
                throw new Exception("Input content was flagged by moderation.");
            }
        }

        foreach (ChatMessagePart part in Input.Parts ?? [])
        {
            if (part.Text is not null)
            {
                ModerationResult modResult = await Client.Moderation.CreateModeration(part.Text);
                if (modResult.Results.FirstOrDefault()?.Flagged == true)
                {
                    throw new Exception("Input content was flagged by moderation.");
                }
            }
        }
    }
}

public class AgentRunnable : OrchestrationRunnable<CombinationalResult<string>, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    public AgentRunnable(TornadoApi client, Orchestration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a friendly chatbot.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Assistant",
            instructions: instructions,
            streaming: streaming);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<CombinationalResult<string>, ChatMessage> process)
    {
        process.RegisterAgent(Agent);

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { new ChatMessage(Code.ChatMessageRoles.User, string.Join('\n',process.Input.Values)) },
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return conv.Messages.Last();
    }
}

public class WebSearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent;

    public WebSearchRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        string instructions = @"You are a websearcher for additional context to the conversation. Please search the web on the provided topic and provide a summary of the results.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Web Searcher",
            instructions: instructions);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(Agent);

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        return conv.Messages.LastOrDefault()?.Content;
    }
}
public class ExitPathRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{


    public ExitPathRunnable(Orchestration orchestrator) : base(orchestrator)
    {
        AllowDeadEnd = true;
    }

    public override ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        Orchestrator?.HasCompletedSuccessfully();
        return new ValueTask<ChatMessage>(process.Input);
    }
}

public class VectorSaveRunnable : OrchestrationRunnable<ChatMessage, ValueTask>
{
    TornadoAgent Agent { get; set; }
    TornadoApi Client { get; set; }

    string ChromaDbURI = "http://localhost:8000/api/v2/";

    public VectorSaveRunnable(TornadoApi client, Orchestration orchestrator, string chromaUri = "http://localhost:8000/api/v2/") : base(orchestrator)
    {
        AllowDeadEnd = true;
        string instructions = @"You are a saver please provide a summary of the following content to be embedded for future reference.";
        Client = client;
        ChromaDbURI = chromaUri;
        Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Vector Saver",
            instructions: instructions);
    }

    public override async ValueTask<ValueTask> Invoke(RunnableProcess<ChatMessage, ValueTask> process)
    {
        process.RegisterAgent(Agent);
        List<VectorDocument> docs = new List<VectorDocument>();

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        if(string.IsNullOrEmpty(process.Input.Content))
        {
            return ValueTask.CompletedTask;
        }

        EmbeddingResult? embeddingResult = await Client.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, process.Input.Content);

        VectorDocument ResultDoc = new VectorDocument(
            Guid.NewGuid().ToString(), 
            process.Input.Content ?? "", 
            new Dictionary<string, object>
            {
                { "source", $"{process.Input.Id}" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            },
            embedding: embeddingResult.Data.FirstOrDefault()?.Embedding
            );

        docs.Add(ResultDoc);

        string latestUserMessage = Orchestrator?.RuntimeProperties.TryGetValue("LatestUserMessage", out var val) == true ? val?.ToString() ?? "" : "";
        if (string.IsNullOrEmpty(latestUserMessage))
        {
            EmbeddingResult? userMessageEmbedding = await Client.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, latestUserMessage);

            VectorDocument InputDoc = new VectorDocument(Guid.NewGuid().ToString(), latestUserMessage, new Dictionary<string, object>
            {
                { "source", $"{process.Input.Id}" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            },
            embedding: userMessageEmbedding?.Data.FirstOrDefault()?.Embedding
            );

            docs.Add(InputDoc);
        }

        await SaveDocument(docs.ToArray());
        return ValueTask.CompletedTask;
    }


    private async Task SaveDocument(VectorDocument[] docs)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(uri: ChromaDbURI);
        await chromaDB.InitializeCollection("ChatBotV2");
        await chromaDB.AddDocumentsAsync(docs);
    }
}

public class VectorSearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent { get; set; }
    TornadoApi Client { get; set; }

    string ChromaDbURI = "http://localhost:8000/api/v2/";

    public VectorSearchRunnable(TornadoApi client, Orchestration orchestrator, string chromaUri = "http://localhost:8000/api/v2/") : base(orchestrator)
    {
        string instructions = @"You are a vector searcher for additional context to the conversation. 
Please provide 2-3 search queries based off the user's input. for quering the vector database";
        Client = client;
        ChromaDbURI = chromaUri;
        Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Vector Searcher",
            outputSchema: typeof(SearchQueries),
            instructions: instructions);
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(Agent);

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });
        SearchQueries? result = conv.Messages.Last().Content.ParseJson<SearchQueries>();
        VectorDocument[] docs = await QueryDB(result?.Queries ?? Array.Empty<string>());
        string combinedContents = string.Join(", ", docs.Select(doc => doc.Content ?? ""));

        Agent.Instructions = $@"You are a friendly chatbot. Use the following pieces of context to answer the question at the end." +
            $"\nIf you don't know the answer, just say that you don't know, don't try to make up an answer." +
            $"\nContext: {combinedContents}" +
            $"\nQuestion: {process.Input.Content}" +
            $"\nAnswer:";

        Agent.OutputSchema = null; // Reset output schema to allow free form answers
        conv = await Agent.RunAsync(combinedContents);
        return conv.Messages.Last().Content;
    }

    private struct SearchQueries
    {
        public string[] Queries { get; set; }
    }

    private async Task<VectorDocument[]> QueryDB(string[] queries)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(uri: ChromaDbURI);
        await chromaDB.InitializeCollection("ChatBotV2");
        List<VectorDocument> results = new List<VectorDocument>();
        foreach (var query in queries)
        {
            EmbeddingResult? embeddingResult = await Client.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, queries);
            var queryEmbedding = await chromaDB.QueryByEmbeddingAsync(embeddingResult?.Data.FirstOrDefault()?.Embedding, topK: 3);
            results.AddRange(queryEmbedding);
        }
        return results.ToArray();
    }
}