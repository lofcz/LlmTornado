using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Intergrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LlmTornado.Demo;

public class VectorDatabasesDemo
{
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
        var queryData = await chromaDB.QueryByEmbeddingAsync(dataQuery, topK: 5);

        foreach (var item in queryData)
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
            var embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel, text);
            return embResult?.Data.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
        }

        public async Task<float[][]> Invoke(string[] contents)
        {
            var embResult = await _tornadoApi.Embeddings.CreateEmbedding(_embeddingModel,contents);
            return embResult?.Data.Select(embedding => embedding.Embedding).ToArray() ?? new float[0][];
        }
    }

    [TornadoTest]
    public static async Task TestParentChildCollection()
    {
        string ChromaDbURI = "http://localhost:8001/api/v2/";
        TornadoChromaDB chromaDB = new TornadoChromaDB(ChromaDbURI);
        TornadoEmbeddingProvider tornadoEmbeddingProvider = new TornadoEmbeddingProvider(Program.Connect(), EmbeddingModel.OpenAi.Gen3.Small);
        await chromaDB.InitializeCollection("ParentCollection");

        MemoryDocumentStore memoryDocumentStore = new MemoryDocumentStore("ParentCollection");
        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        string text = File.ReadAllText("Static/Files/pride_and_prejudice.txt");

        await pcdRetriever.CreateParentChildCollection(text, 1000, 200, 300, 50, tornadoEmbeddingProvider);

        string query = "that a single man in possession of a good fortune";
        var queryEmb = await tornadoEmbeddingProvider.Invoke(query);

        var result = await pcdRetriever.SearchAsync(queryEmb);

        foreach(var doc in result)
        {
            Console.WriteLine($"Content: {doc.Content}\n");
        }
    }

}
