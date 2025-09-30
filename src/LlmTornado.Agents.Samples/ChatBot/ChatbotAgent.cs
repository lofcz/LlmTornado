using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Infra;
using LlmTornado.Moderation;
using LlmTornado.Responses;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Intergrations;

using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LlmTornado.Agents.Samples.ChatBot;

public class ChatBotAgent : OrchestrationRuntimeConfiguration
{
    TornadoApi Client { get; set; }
    public ChatBotAgent()
    {
        Client = new TornadoApi(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        BuildComplexAgent(Client, true, "http://localhost:8001/api/v2/", "AgentV10.json", "AgentV10");
    }

    public OrchestrationRuntimeConfiguration BuildSimpleAgent(TornadoApi client, bool streaming = false, string conversationFile = "SimpleAgent.json")
    {
        ModeratorRunnable inputModerator = new ModeratorRunnable(client, this);

        SimpleAgentRunnable simpleAgentRunnable = new SimpleAgentRunnable(client, this, streaming);

        ExitPathRunnable exitPathRunnable = new ExitPathRunnable(this);

        return new OrchestrationBuilder(this)
           .SetEntryRunnable(inputModerator)
           .SetOutputRunnable(simpleAgentRunnable)
           .WithRuntimeInitializer((config) =>
           {
               simpleAgentRunnable.OnAgentRunnerEvent += (sEvent) =>
               {
                   // Forward agent runner events (including streaming) to runtime
                   config.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, config.Runtime?.Id ?? string.Empty));
               };
               return ValueTask.CompletedTask;
           })
           .WithRuntimeProperty("LatestUserMessage", "")
           .WithChatMemory(conversationFile)
           .AddAdvancer<ChatMessage>(inputModerator, simpleAgentRunnable)
           .AddAdvancer<ChatMessage>(simpleAgentRunnable, exitPathRunnable)
           .AddExitPath<ChatMessage>(exitPathRunnable, _ => true)
           .CreateDotGraphVisualization("SimpleChatBotAgent.dot").Build();
    }

    public OrchestrationRuntimeConfiguration BuildComplexAgent(TornadoApi client, bool streaming = false, string chromaUri = "http://localhost:8001/api/v2/",string conversationFile = "AgentV10.json", string withLongtermMemoryID = "AgentV10")
    {
        AgentRunnable RunnableAgent = new AgentRunnable(client, this, streaming);
        ModeratorRunnable inputModerator = new ModeratorRunnable(client, this);
        VectorSearchRunnable vectorSearchRunnable = new VectorSearchRunnable(client, this, chromaUri);
        WebSearchRunnable webSearchRunnable = new WebSearchRunnable(client, this);
        VectorSaveRunnable vectorSaveRunnable = new VectorSaveRunnable(client, this, chromaUri);
        ExitPathRunnable exitPathRunnable = new ExitPathRunnable(this);
        VectorEntitySaveRunnable vectorEntitySaveRunnable = new VectorEntitySaveRunnable(client, this, chromaUri);
        ChatPassthruRunnable chatPassthruRunnable = new ChatPassthruRunnable(this);

        return new OrchestrationBuilder(this)
            .SetEntryRunnable(inputModerator)
            .SetOutputRunnable(RunnableAgent)
            .WithRuntimeInitializer((config) =>
            {
                RunnableAgent.OnAgentRunnerEvent += (sEvent) =>
                {
                    // Forward agent runner events (including streaming) to runtime
                    config.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, config.Runtime?.Id ?? string.Empty));
                };
                return ValueTask.CompletedTask;
            })
            .WithRuntimeProperty("LatestUserMessage", "")
            .WithRuntimeProperty("MemoryCollectionName", $"{withLongtermMemoryID}")
            .WithRuntimeProperty("EntitiesCollectionName", $"{withLongtermMemoryID}Entities")
            .WithChatMemory(conversationFile)
            .WithDataRecording()
            .AddParallelAdvancement(inputModerator,
                new OrchestrationAdvancer<ChatMessage>(webSearchRunnable),
                new OrchestrationAdvancer<ChatMessage>(vectorSearchRunnable),
                new OrchestrationAdvancer<ChatMessage>(vectorEntitySaveRunnable),
                new OrchestrationAdvancer<ChatMessage>(chatPassthruRunnable))
            .AddCombinationalAdvancement<string>(
                fromRunnables: [webSearchRunnable, vectorSearchRunnable, vectorEntitySaveRunnable, chatPassthruRunnable],
                condition: _ => true,
                toRunnable: RunnableAgent,
                requiredInputToAdvance: 4,
                combinationRunnableName: "CombinationalContextWaiter")
            .AddParallelAdvancement(RunnableAgent,
                new OrchestrationAdvancer<ChatMessage>(vectorSaveRunnable),
                new OrchestrationAdvancer<ChatMessage>(exitPathRunnable))
            .AddExitPath<ChatMessage>(exitPathRunnable, _ => true)
            .CreateDotGraphVisualization("ChatBotAgent.dot")
            .Build();
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

        try
        {
            Orchestrator?.RuntimeProperties.AddOrUpdate("LatestUserMessage", (newValue) => input.Input.Content ?? "", (key, Value) => input.Input.Content ?? "");
        }
        catch(Exception e) {
            Console.WriteLine(e.Message);
            throw;
        }

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

    Conversation _conv;
    OrchestrationRuntimeConfiguration _runtimeConfiguration;
    public AgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a conversational chatbot, be engaging and creative to have a playful and interesting conversation with the user.
Given the following context will include Vector Search Memory, Websearch Results, and Entity Memory to keep track of real world things.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Assistant",
            instructions: instructions,
            streaming: streaming);
        _conv = Agent.Client.Chat.CreateConversation(Agent.Options);
        _runtimeConfiguration = orchestrator;
    }


    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<CombinationalResult<string>, ChatMessage> process)
    {
        process.RegisterAgent(Agent);


        string prompt = string.Join("\n\n", process.Input.Values);

        _conv = await Agent.RunAsync(
            input: prompt,
            appendMessages: _runtimeConfiguration.GetMessages(),
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return _conv.Messages.Last();
    }
}

public class SimpleAgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    OrchestrationRuntimeConfiguration _runtimeConfiguration;

    Conversation _conv;

    public SimpleAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a friendly chatbot. Given the following context and users prompt generate a response to the user that is helpful and informative.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Assistant",
            instructions: instructions,
            streaming: streaming);

        _runtimeConfiguration = orchestrator;
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        process.RegisterAgent(Agent);

        _conv = await Agent.RunAsync(
            appendMessages: _runtimeConfiguration.GetMessages(),
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return _conv.Messages.Last();
    }
}

public class WebSearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent;

    public WebSearchRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        string instructions = @"You are a websearcher for additional context to the conversation. Please search the web on the provided topic and provide a summary of the results. If nothing is relevant for the conversation just say so.";

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

        return "WEB SEARCH CONTEXT: " +  conv.Messages.LastOrDefault()?.Content;
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
        _ = Task.Run(() => BackgroundTaskSaveVectorDocs(process.Input));
        return ValueTask.CompletedTask;
    }

    private async Task BackgroundTaskSaveVectorDocs(ChatMessage message)
    {
        Orchestrator.RuntimeProperties.TryGetValue("MemoryCollectionName", out var colName);

        if (colName != null && !string.IsNullOrEmpty(colName.ToString()))
        {
            collectionName = colName.ToString() ?? collectionName;
        }

        string messageId = message.Id.ToString() ?? Guid.NewGuid().ToString();

        _ = Task.Run(async () =>
        {
            await SaveLastUserMessage(messageId);
        });

        _ = Task.Run(async () =>
        {
            await SaveLastAssistantMessage(message);
        });
    }

    private async Task SaveLastAssistantMessage(ChatMessage message)
    {
        //Creates a summary of the assistant's response to be saved.
        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { message });
        if (!string.IsNullOrEmpty(message.Content))
        {
            await SaveDocument(message.Content, additionalStaticParentMetadata: new Dictionary<string, object>()
                {
                    { "Role","Assistant" },
                    { "ResponseId", message.Id },
                    { "Timestamp", DateTime.UtcNow }
                });
        }
    }

    private async Task SaveLastUserMessage(string messageId)
    {
        string latestUserMessage = Orchestrator?.RuntimeProperties.TryGetValue("LatestUserMessage", out var val) == true ? val?.ToString() ?? "" : "";
        if (!string.IsNullOrEmpty(latestUserMessage))
            await SaveDocument(latestUserMessage, additionalStaticParentMetadata: new Dictionary<string, object>()
                {
                    {"Role","User" },
                    {"ResponseId", messageId },
                    {"Timestamp", DateTime.UtcNow }
                });
    }

    private async Task SaveDocument(string text, Dictionary<string, object>? additionalStaticParentMetadata = null, Dictionary<string, object>? additionalStaticChildMetadata = null)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        await chromaDB.InitializeCollection(collectionName);

        IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);

        LocalDocumentStore memoryDocumentStore = new LocalDocumentStore(Directory.GetCurrentDirectory(), collectionName);

        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        await pcdRetriever.CreateParentChildCollection(text, 2000, 250, 500, 125, tornadoEmbeddingProvider);
    }
}
public class ChatPassthruRunnable : OrchestrationRunnable<ChatMessage, string>
{
    public ChatPassthruRunnable(Orchestration orchestrator) : base(orchestrator)
    {
    }

    public override ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        Orchestrator.RuntimeProperties.TryUpdate("LatestUserMessage", process.Input.Content ?? "", Orchestrator.RuntimeProperties["LatestUserMessage"]?.ToString() ?? "");
        return new ValueTask<string>("USERS ORIGINAL MESSAGE: " + process.Input.Content);
    }
}

public class VectorSearchRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent { get; set; }
    TornadoApi Client { get; set; }

    string ChromaDbURI = "http://localhost:8000/api/v2/";
    string collectionName;

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
        else
        {
            Console.WriteLine("No collection name found in runtime properties");
            return "VECTOR DB CONTEXT: ";
        }

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        SearchQueries? result = conv.Messages.Last().Content.ParseJson<SearchQueries>();

        VectorDocument[] docs = await QueryDB(result?.Queries ?? Array.Empty<string>());

        string combinedContents = string.Join(", ", docs.Distinct().Select(doc => doc.Content ?? ""));

        return "VECTOR DB CONTEXT: " + combinedContents ;
    }

    private struct SearchQueries
    {
        public string[] Queries { get; set; }
    }

    private async Task<VectorDocument[]> QueryDB(string[] queries)
    {
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);

        await chromaDB.InitializeCollection(collectionName);

        LocalDocumentStore memoryDocumentStore = new LocalDocumentStore(Directory.GetCurrentDirectory(), collectionName);
        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        List<VectorDocument> results = new List<VectorDocument>();

        foreach (var query in queries)
        {
            var queryEmb = await tornadoEmbeddingProvider.Invoke(query);

            var result = await pcdRetriever.SearchAsync(queryEmb, topK:3);

            foreach (var doc in result)
            {
                results.Add((VectorDocument)doc);
            }
            
        }

        return results.ToArray();
    }
}

public struct KeyValue
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public enum EntityTypes
{
    Person,
    Place,
    Thing,
    Organization,
    Event,
    Concept,
    Other
}

[Description("Used for storing information about real world objects")]
public struct Entity
{
    [Description("Name for the Entity (Jake, My Fridge, work)")]
    public string EntityName { get; set; }

    [Description("Context of the Entity, description of the entity and the relation to the owner")]
    public string Context { get; set; }

    [Description("Type of Entity, Person, Place, Thing, Organization, Event, Concept, Other")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public EntityTypes EntityType { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
    [Description("Unique Properties that can be assigned to this entity for additional context")]
    public KeyValue[]? Properties { get; set; }

    [Description("Entity Name who owns this Entity or N/A if unknown")]
    public string EntityOwner { get; set; }

    public override string ToString()
    {
        return $"{EntityName}\nContext: {Context}\nType: {EntityType}\nOwner: {EntityOwner}";
    }
}

[Description("List of all entities detected in the user's message")]
public struct Entities
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
    public Entity[]? DetectedEntities { get; set; }
}

[Description("List of all entities detected in the user's message")]
public struct SortedEntities
{
    [Description("Entities that need to be updated with new information")]
    public Entity[]? EntitiesToUpdate { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
    [Description("New Entities that do not exist in the vector database")]
    public Entity[]? NewEntities { get; set; }
}

public class VectorEntitySaveRunnable : OrchestrationRunnable<ChatMessage, string>
{
    TornadoAgent Agent { get; set; }

    TornadoApi Client { get; set; }

    string ChromaDbURI = "http://localhost:8001/api/v2/";

    string collectionName = "ChatBotEntitiesV3";
    ConcurrentBag<VectorDocument> newEntities = new ConcurrentBag<VectorDocument>();
    ConcurrentBag<VectorDocument> knownEntities = new ConcurrentBag<VectorDocument>();
    public VectorEntitySaveRunnable(TornadoApi client, Orchestration orchestrator, string chromaUri = "http://localhost:8001/api/v2/") : base(orchestrator)
    {
        AllowDeadEnd = true;
        string instructions = @"You are an expert Entity Identifier. Your job is to detect all the entities in the user's message and extract their context for saving";
        Client = client;
        ChromaDbURI = chromaUri;
        
        Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Vector Entity Saver",
            outputSchema: typeof(Entities),
            instructions: instructions);
    }

    public override async ValueTask InitializeRunnable()
    {
        Orchestrator.RuntimeProperties.TryGetValue("EntitiesCollectionName", out var colName);

        if (colName != null && !string.IsNullOrEmpty(colName.ToString()))
        {
            collectionName = colName.ToString() ?? collectionName;
        }

        newEntities = new ConcurrentBag<VectorDocument>();
        knownEntities = new ConcurrentBag<VectorDocument>();
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(Agent);
        
        //Gather list of entities from user input
        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        if (string.IsNullOrEmpty(process.Input.Content)) return "ENTITIES: {}";

        Entities entities = conv.Messages.Last().Content.ParseJson<Entities>();

        if(entities.DetectedEntities == null) return "ENTITIES: {}";

        RunKnownEntityDetector(entities);

        string response = "ENTITIES: " + string.Join("\n\n", knownEntities.Select(entity => entity.ToString()));

        BackgroundTaskUpdateKnownEntities(knownEntities.ToArray(), ChromaDbURI, collectionName);
        BackgroundTaskSaveNewEntities(newEntities.ToArray(), ChromaDbURI, collectionName);

        return response;
    }

    private void RunKnownEntityDetector(Entities entities)
    {
        if(entities.DetectedEntities == null) return;
        Parallel.ForEach(entities.DetectedEntities, async (entity) =>
        {
            List<VectorDocument> similarEntities = new List<VectorDocument>(await GetSimilarEntities(entity, ChromaDbURI, collectionName));
            VectorDocument existingDoc = similarEntities.FirstOrDefault(ety => ety.Metadata["EntityName"].ToString() == entity.EntityName);
            if (existingDoc != null)
            {
                IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);
                existingDoc.Embedding = tornadoEmbeddingProvider.Invoke(existingDoc.Content ?? "").GetAwaiter().GetResult();
                existingDoc = UpdateFromEntity(existingDoc, entity);
                knownEntities.Add(existingDoc);
                return;
            }
            newEntities.Add(CreateFromEntity(entity));
        });
    }

    private async Task<VectorDocument[]> GetSimilarEntities(Entity entity, string uri, string collectionName)
    {
        IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);
        TornadoChromaDB chromaDB = new TornadoChromaDB(uri);
       

        await chromaDB.InitializeCollection(collectionName);
        float[] embedding = await tornadoEmbeddingProvider.Invoke(entity.ToString());
        return await chromaDB.QueryByEmbeddingAsync(embedding, topK: 5);
    }

    private void BackgroundTaskUpdateKnownEntities(VectorDocument[] docs, string uri, string collectionName)
    {
        _ = Task.Run(async () =>
        {
            IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);
            TornadoChromaDB chromaDB = new TornadoChromaDB(uri);
            await chromaDB.InitializeCollection(collectionName);
            await chromaDB.UpdateDocumentsAsync(docs.ToArray());
        });
    }

    private void BackgroundTaskSaveNewEntities(VectorDocument[] newEntities, string uri, string collectionName)
    {
        _ = Task.Run(async () =>
        {
            IDocumentEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Client, EmbeddingModel.OpenAi.Gen3.Small);
            TornadoChromaDB chromaDB = new TornadoChromaDB(uri);

            await chromaDB.InitializeCollection(collectionName);

            foreach (var doc in newEntities)
            {
                float[] embedding = await tornadoEmbeddingProvider.Invoke(doc.Content ?? "");
                doc.Embedding = embedding;
            }

            await chromaDB.AddDocumentsAsync(newEntities.ToArray());
        });
    }

    private VectorDocument CreateFromEntity(Entity entity, float[]? data = null)
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>();
        foreach (var prop in entity.Properties)
        {
            if (!metadata.ContainsKey(prop.Key))
            {
                metadata.Add(prop.Key, prop.Value);
            }
        }
        metadata.Add("EntityType", entity.EntityType.ToString());
        metadata.Add("EntityOwner", entity.EntityOwner);
        metadata.Add("EntityName", entity.EntityName);
        metadata.Add("CreateDate", DateTime.UtcNow);
        metadata.Add("ModDate", DateTime.UtcNow);

        return new VectorDocument(
            id: Guid.NewGuid().ToString(),
            content: entity.ToString(),
            metadata: metadata,
            embedding: data);
    }

    private VectorDocument UpdateFromEntity(VectorDocument docToUpdate, Entity entity, float[]? data = null)
    {
        Dictionary<string, object> metadata = docToUpdate.Metadata ?? new Dictionary<string, object>();

        foreach (var prop in entity.Properties)
        {
            if (!metadata.ContainsKey(prop.Key))
            {
                metadata.Add(prop.Key, prop.Value);
            }
            else
            {
                metadata[prop.Key] = prop.Value;
            }
        }

        metadata["ModDate"] = DateTime.UtcNow;

        return new VectorDocument(
            id: Guid.NewGuid().ToString(),
            content: entity.ToString(),
            metadata: metadata,
            embedding: data);
    }

}

public class TornadoEmbeddingProvider : IDocumentEmbeddingProvider
{
    private TornadoApi _tornadoApi;
    private EmbeddingModel _embeddingModel;
    public TornadoEmbeddingProvider(TornadoApi tornadoApi, EmbeddingModel embeddingModel)
    {
        _tornadoApi = tornadoApi;
        _embeddingModel = embeddingModel;
    }
    public async Task<float[]> Invoke(string text)
    {
        var embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel, text);
        return embResult?.Data.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
    }

    public async Task<float[][]> Invoke(string[] contents)
    {
        var embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel, contents);
        return embResult?.Data.Select(embedding => embedding.Embedding).ToArray() ?? new float[0][];
    }
}
