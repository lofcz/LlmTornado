using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Intergrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.VectorDatabases.Pinecone;
using LlmTornado.VectorDatabases.Pinecone.Integrations;
using static System.Net.Mime.MediaTypeNames;

namespace LlmTornado.Demo;

public class VectorDatabasesDemo
{
    [TornadoTest, Flaky("requires specific index")]
    public static async Task TestPinecone()
    {
        TornadoPinecone pinecone = new TornadoPinecone(new PineconeConfigurationOptions(Program.ApiKeys.Pinecone)
        {
            IndexName = "dancing-poplar",
            Dimension = 1024,
            Cloud = PineconeCloud.Aws,
            Region = "us-east-1"
        });

        pinecone.PineconeClient.Client
        
        await pinecone.DeleteAllDocumentsAsync();

        await pinecone.AddDocumentsAsync([
            new VectorDocument(
                id: "doc1", 
                content: "Apple is a popular fruit known for its sweetness and crisp texture."
            ),
            new VectorDocument(
                id: "doc2",
                content: "The tech company Apple is known for its innovative products like the iPhone."
            ),
            new VectorDocument(
                id: "doc3",
                content: "Many people enjoy eating apples as a healthy snack."
            )
        ]);

        // it takes a few seconds for the newly inserted docs to be searchable and this isn't exposed via api
        await Task.Delay(10_000);
        
        string searchQuery = "which company is known for iphone?";
        float[] queryEmbedding = await pinecone.EmbedAsync(searchQuery);
        
        VectorDocument[] results = await pinecone.QueryByEmbeddingAsync(queryEmbedding);

        Console.WriteLine($"Search query: '{searchQuery}'");
        Console.WriteLine("Top results:");
        foreach (VectorDocument result in results)
        {
            Console.WriteLine($"  - ID: {result.Id}");
            Console.WriteLine($"    Content: {result.Content}");
            Console.WriteLine($"    Score: {result.Score:F4}\n");
        }
    }

    [TornadoTest]
    public static async Task TestChromaDB()
    {
        string query = "Function to add two numbers in python";
        string embeddingDescription = "A function that adds two numbers together in python";

        Dictionary<string, object> metaData = new Dictionary<string, object>();
        metaData.Add("FunctionName", "Function 1");

        string ChromaDbURI = "http://localhost:8001/api/v2/";
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        await chromaDB.InitializeCollection("functions");

        //Embed the function description and add to DB
        TornadoApi tornadoApi = Program.Connect();
        List<Task> tasks = new List<Task>();

        EmbeddingResult? embInputResult;
        EmbeddingResult? embQueryResult;
        float[]? dataInput = [];
        float[]? dataQuery = [];

        tasks.Add(Task.Run(async () => { embInputResult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, embeddingDescription); dataInput = embInputResult?.Data.FirstOrDefault()?.Embedding; }));
        tasks.Add(Task.Run(async () => { embQueryResult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, query); dataQuery = embQueryResult?.Data.FirstOrDefault()?.Embedding; }));

        await Task.WhenAll(tasks);

        //Add document to DB
        VectorDocument vectorDocument = new VectorDocument(Guid.NewGuid().ToString(), embeddingDescription, metaData, dataInput);

        await chromaDB.AddDocumentsAsync([vectorDocument]);

        //Query DB for relevant functions
        VectorDocument[] queryData = await chromaDB.QueryByEmbeddingAsync(dataQuery, topK: 5);

        foreach (VectorDocument item in queryData)
        {
            Console.WriteLine($"Function Name: {item.Metadata?["FunctionName"]} \n Description: {item.Content}\n\n");
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
            EmbeddingResult? embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel, text);
            return embResult?.Data.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
        }

        public async Task<float[][]> Invoke(string[] contents)
        {
            EmbeddingResult? embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel,contents);
            return embResult?.Data.Select(embedding => embedding.Embedding).ToArray() ?? new float[0][];
        }
    }

    [TornadoTest]
    public static async Task TestParentChildCollection()
    {
        string ChromaDbURI = "http://localhost:8001/api/v2/";
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);
        string collectionName = $"ParentChildCollection_{Guid.NewGuid().ToString().Substring(0,4)}";
        await chromaDB.InitializeCollection(collectionName);

        MemoryDocumentStore memoryDocumentStore = new MemoryDocumentStore(collectionName);
        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        string text = File.ReadAllText("Static/Files/pride_and_prejudice.txt");

        await pcdRetriever.CreateParentChildCollection(text, 2000, 200, 1000, 100, tornadoEmbeddingProvider);

        string query = "that a single man in possession of a good fortune";
        float[] queryEmb = await tornadoEmbeddingProvider.Invoke(query);

        IEnumerable<Document> result = await pcdRetriever.SearchAsync(queryEmb);

        foreach(Document doc in result)
        {
            VectorDocument vectorDoc = (VectorDocument)doc;
            Console.WriteLine($"Content: {vectorDoc.Content}\n");
        }
    }


    [TornadoTest]
    public static async Task TestCasting()
    {
        VectorDocument vectorDocument = new VectorDocument("1", "This is a test document", new Dictionary<string, object> { { "Author", "John Doe" } }, new float[] { 0.1f, 0.2f, 0.3f });
        Document doc = (Document)vectorDocument;

        VectorDocument vectorDocument2 = (VectorDocument)doc;
        Console.WriteLine($"VectorDocument2 Id: {vectorDocument2.Id}, Content: {vectorDocument2.Content}, Author: {vectorDocument2.Metadata["Author"]}");

    }

}
