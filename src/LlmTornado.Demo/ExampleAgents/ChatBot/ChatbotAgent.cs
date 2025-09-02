using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Moderation;
using LlmTornado.Responses;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Intergrations;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using static LlmTornado.Demo.VectorDatabasesDemo;

namespace LlmTornado.Demo.ExampleAgents.ChatBot;

public class ChatbotAgent : OrchestrationRuntimeConfiguration
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
            .WithRuntimeProperty("MemoryCollectionName", "AgentV3")
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
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();
        Configuration.Runtime = this.Runtime;
        this.Runtime.RuntimeConfiguration = Configuration;
        Configuration.OnRuntimeInitialized();
        RunnableAgent.OnAgentRunnerEvent += (sEvent) =>
        {
            // Forward agent runner events (including streaming) to runtime
            this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime?.Id ?? string.Empty));
        };
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

    string collectionName = "ChatBotV2";

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

        Orchestrator.RuntimeProperties.TryGetValue("MemoryCollectionName", out var colName);

        if (colName != null && !string.IsNullOrEmpty(colName.ToString()))
        {
            collectionName = colName.ToString() ?? collectionName;
        }

        List<Task> tasks = new List<Task>();
        //Saves the latest user message to the vector database along with the ResponseID of the assistant's response.
       
        tasks.Add(Task.Run(async () =>
        {
            string latestUserMessage = Orchestrator?.RuntimeProperties.TryGetValue("LatestUserMessage", out var val) == true ? val?.ToString() ?? "" : "";
            if (!string.IsNullOrEmpty(latestUserMessage))
                await SaveDocument(latestUserMessage, additionalStaticParentMetadata: new Dictionary<string, object>()
                {
                    {"Role","User" },
                    {"ResponseId", process.Input.Id },
                    {"Timestamp", DateTime.UtcNow }
                });
        }));
        
        tasks.Add(Task.Run(async () =>
        {
            //Creates a summary of the assistant's response to be saved.
            Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

            if (!string.IsNullOrEmpty(process.Input.Content))
            {
                await SaveDocument(process.Input.Content, additionalStaticParentMetadata: new Dictionary<string, object>()
                {
                    { "Role","Assistant" },
                    { "ResponseId", process.Input.Id },
                    { "Timestamp", DateTime.UtcNow }
                });
            }  
        }));
        
        await Task.WhenAll(tasks);

        return ValueTask.CompletedTask;
    }


    private async Task SaveDocument(string text, Dictionary<string, object>? additionalStaticParentMetadata = null, Dictionary<string, object>? additionalStaticChildMetadata = null)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        await chromaDB.InitializeCollection(collectionName);

        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);
        
        MemoryDocumentStore memoryDocumentStore = new MemoryDocumentStore(collectionName);

        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        await pcdRetriever.CreateParentChildCollection(text, 2000, 250, 500, 125, tornadoEmbeddingProvider);
    }
}

public class VectorSearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent { get; set; }
    TornadoApi Client { get; set; }

    string ChromaDbURI = "http://localhost:8000/api/v2/";
    string collectionName = "ChatBotV2";

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

        Orchestrator.RuntimeProperties.TryGetValue("MemoryCollectionName", out var colName);

        if (colName != null && !string.IsNullOrEmpty(colName.ToString()))
        {
            collectionName = colName.ToString() ?? collectionName;
        }

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        SearchQueries? result = conv.Messages.Last().Content.ParseJson<SearchQueries>();

        VectorDocument[] docs = await QueryDB(result?.Queries ?? Array.Empty<string>());

        string combinedContents = string.Join(", ", docs.Select(doc => doc.Content ?? ""));

        return combinedContents;
    }

    private struct SearchQueries
    {
        public string[] Queries { get; set; }
    }

    private async Task<VectorDocument[]> QueryDB(string[] queries)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);

        await chromaDB.InitializeCollection(collectionName);

        MemoryDocumentStore memoryDocumentStore = new MemoryDocumentStore(collectionName);
        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        List<VectorDocument> results = new List<VectorDocument>();

        foreach (var query in queries)
        {
            var queryEmb = await tornadoEmbeddingProvider.Invoke(query);

            var result = await pcdRetriever.SearchAsync(queryEmb, topK:3);

            results.AddRange(result.Cast<VectorDocument>());
        }

        return results.ToArray();
    }
}