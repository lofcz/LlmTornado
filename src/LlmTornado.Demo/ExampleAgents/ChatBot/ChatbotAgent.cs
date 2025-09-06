using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Infra;
using LlmTornado.Moderation;
using LlmTornado.Responses;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Intergrations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using static LlmTornado.Demo.ExampleAgents.ChatBot.VectorEntitySaveRunnable;
using static LlmTornado.Demo.VectorDatabasesDemo;

namespace LlmTornado.Demo.ExampleAgents.ChatBot;

public class ChatBotAgent
{
    public OrchestrationRuntimeConfiguration BuildSimpleAgent(TornadoApi client, bool streaming = false)
    {
        OrchestrationBuilder builder = new OrchestrationBuilder();

        ModeratorRunnable inputModerator = new ModeratorRunnable(client, builder.Configuration);

        SimpleAgentRunnable simpleAgentRunnable = new SimpleAgentRunnable(client, builder.Configuration, streaming);

        ExitPathRunnable exitPathRunnable = new ExitPathRunnable(builder.Configuration);

        builder
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
           .WithChatMemory("SimpleAgent.json")
           .AddAdvancer<ChatMessage>(inputModerator, simpleAgentRunnable)
           .AddAdvancer<ChatMessage>(simpleAgentRunnable, exitPathRunnable)
           .AddExitPath<ChatMessage>(exitPathRunnable, _ => true)
           .CreateDotGraphVisualization("SimpleChatBotAgent.dot");

        return builder.Build();
    }

    public OrchestrationRuntimeConfiguration BuildComplexAgent(TornadoApi client, bool streaming = false, string chromaUri = "http://localhost:8001/api/v2/")
    {
        OrchestrationBuilder builder = new OrchestrationBuilder();

        AgentRunnable RunnableAgent = new AgentRunnable(client, builder.Configuration, streaming);
        ModeratorRunnable inputModerator = new ModeratorRunnable(client, builder.Configuration);
        VectorSearchRunnable vectorSearchRunnable = new VectorSearchRunnable(client, builder.Configuration, chromaUri);
        WebSearchRunnable webSearchRunnable = new WebSearchRunnable(client, builder.Configuration);
        VectorSaveRunnable vectorSaveRunnable = new VectorSaveRunnable(client, builder.Configuration, chromaUri);
        ExitPathRunnable exitPathRunnable = new ExitPathRunnable(builder.Configuration);
        VectorEntitySaveRunnable vectorEntitySaveRunnable = new VectorEntitySaveRunnable(client, builder.Configuration, chromaUri);
        ChatPassthruRunnable chatPassthruRunnable = new ChatPassthruRunnable(builder.Configuration);
        builder.SetEntryRunnable(inputModerator)
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
            .WithRuntimeProperty("MemoryCollectionName", "AgentV9")
            .WithRuntimeProperty("EntitiesCollectionName", "AgentEntitiesV9")
            .WithChatMemory("AgentV9.json")
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
            .CreateDotGraphVisualization("ChatBotAgent.dot");

        return builder.Build();
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
        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);

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

    TornadoChromaDB chromaDB { get; set; }


    public VectorEntitySaveRunnable(TornadoApi client, Orchestration orchestrator, string chromaUri = "http://localhost:8001/api/v2/") : base(orchestrator)
    {
        AllowDeadEnd = true;
        string instructions = @"You are an expert Entity Identifier. Your job is to detect all the entities in the user's message and extract their context for saving";
        Client = client;
        ChromaDbURI = chromaUri;
        chromaDB = new TornadoChromaDB(ChromaDbURI);
        Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Vector Entity Saver",
            outputSchema: typeof(Entities),
            instructions: instructions);
    }

    public override async ValueTask<string> Invoke(RunnableProcess<ChatMessage, string> process)
    {
        process.RegisterAgent(Agent);
        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);

        Orchestrator.RuntimeProperties.TryGetValue("EntitiesCollectionName", out var colName);

        if (colName != null && !string.IsNullOrEmpty(colName.ToString()))
        {
            collectionName = colName.ToString() ?? collectionName;
        }

        await chromaDB.InitializeCollection(collectionName);

        //Creates a summary of the assistant's response to be saved.
        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { process.Input });

        if (!string.IsNullOrEmpty(process.Input.Content))
        {
            Entities entities = conv.Messages.Last().Content.ParseJson<Entities>();

            List<VectorDocument> similarEntities = new List<VectorDocument>();
            foreach (var entity in entities.DetectedEntities)
            {
                
                float[] embedding = await tornadoEmbeddingProvider.Invoke(entity.ToString());
                
                VectorDocument[] existingEntities = await QueryEntities(embedding);
                similarEntities.AddRange(existingEntities);
            }

            if (entities.DetectedEntities.Length == 0)
                return "ENTITIES: {}";



            List<VectorDocument> newEntities = new List<VectorDocument>();
            List<VectorDocument> knownEntities = new List<VectorDocument>();

            foreach (var entity in entities.DetectedEntities)
            {
                VectorDocument existingDoc = similarEntities.FirstOrDefault(ety => ety.Metadata["EntityName"].ToString() == entity.EntityName);
                
                if (existingDoc != null)
                {
                    existingDoc.Embedding = await tornadoEmbeddingProvider.Invoke(existingDoc.Content ?? "");
                    knownEntities.Add(UpdateFromEntity(existingDoc, entity));
                    await UpdateDocuments(new[] { UpdateFromEntity(existingDoc, entity) });
                    continue;
                }
                newEntities.Add(CreateFromEntity(entity));
            }

            foreach (var doc in newEntities)
            {
                float[] embedding = await tornadoEmbeddingProvider.Invoke(doc.Content ?? "");
                doc.Embedding = embedding;
            }

            await SaveDocuments(newEntities.ToArray());

            string resultResult = "ENTITIES: " + string.Join("\n\n", knownEntities.Select(entity => entity.ToString()));

            return resultResult;
        }

        return "ENTITIES: {}";
    }

    private async Task<VectorDocument[]> QueryEntities(float[] embedding)
    {
        return await chromaDB.QueryByEmbeddingAsync(embedding, topK: 5);
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
    private async Task SaveDocuments(VectorDocument[] entities)
    {
        await chromaDB.AddDocumentsAsync(entities);
    }

    private async Task UpdateDocuments(VectorDocument[] entities)
    {
        await chromaDB.UpdateDocumentsAsync(entities);
    }
}
