using LlmTornado.VectorDatabases.Intergrations;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}
