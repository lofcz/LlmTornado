using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.VectorDatabases;
using LlmTornado.VectorDatabases.Faiss.Integrations;
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
        string collectionName = $"ParentChildCollection_{Guid.NewGuid().ToString().Substring(0,4)}";
        await chromaDB.InitializeCollection(collectionName);

        MemoryDocumentStore memoryDocumentStore = new MemoryDocumentStore(collectionName);
        ParentChildDocumentRetriever pcdRetriever = new ParentChildDocumentRetriever(chromaDB, memoryDocumentStore);

        string text = File.ReadAllText("Static/Files/pride_and_prejudice.txt");

        await pcdRetriever.CreateParentChildCollection(text, 2000, 200, 1000, 100, tornadoEmbeddingProvider);

        string query = "that a single man in possession of a good fortune";
        var queryEmb = await tornadoEmbeddingProvider.Invoke(query);

        var result = await pcdRetriever.SearchAsync(queryEmb);

        foreach(var doc in result)
        {
            var vectorDoc = (VectorDocument)doc;
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

    [TornadoTest]
    public static async Task TestInMemoryVectorDB()
    {
        // Initialize the FAISS vector database
        var faissDb = new FaissVectorDatabase(
            indexDirectory: "./faiss_example_indexes",
            vectorDimension: 128 // Using smaller dimension for example
        );

        // Initialize a collection
        await faissDb.InitializeCollection("example_collection");

        // Create some sample documents with embeddings
        var documents = new[]
        {
            new VectorDocument(
                id: "doc1",
                content: "The quick brown fox jumps over the lazy dog",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object>
                {
                    ["category"] = "animals",
                    ["year"] = 2024
                }
            ),
            new VectorDocument(
                id: "doc2",
                content: "Machine learning is a subset of artificial intelligence",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object>
                {
                    ["category"] = "technology",
                    ["year"] = 2024
                }
            ),
            new VectorDocument(
                id: "doc3",
                content: "The cat sat on the mat",
                embedding: GenerateRandomEmbedding(128),
                metadata: new Dictionary<string, object>
                {
                    ["category"] = "animals",
                    ["year"] = 2023
                }
            )
        };

        // Add documents
        Console.WriteLine("Adding documents...");
        await faissDb.AddDocumentsAsync(documents);
        Console.WriteLine($"Added {documents.Length} documents");

        // Get documents by ID
        Console.WriteLine("\nGetting documents by ID...");
        var retrievedDocs = await faissDb.GetDocumentsAsync(new[] { "doc1", "doc2" });
        foreach (var doc in retrievedDocs)
        {
            Console.WriteLine($"  {doc.Id}: {doc.Content}");
        }

        // Query by embedding (using first document's embedding as query)
        Console.WriteLine("\nQuerying by embedding...");
        var queryEmbedding = documents[0].Embedding!;
        var results = await faissDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            topK: 3,
            includeScore: true
        );

        Console.WriteLine("Top 3 results:");
        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Id}: {result.Content} (Distance: {result.Score:F4})");
        }

        // Query with metadata filter
        Console.WriteLine("\nQuerying with metadata filter (category = animals)...");
        var whereFilter = TornadoWhereOperator.Equal("category", "animals");
        var filteredResults = await faissDb.QueryByEmbeddingAsync(
            embedding: queryEmbedding,
            where: whereFilter,
            topK: 3,
            includeScore: true
        );

        Console.WriteLine("Filtered results:");
        foreach (var result in filteredResults)
        {
            Console.WriteLine($"  {result.Id}: {result.Content} (Distance: {result.Score:F4})");
        }

        // Update a document
        Console.WriteLine("\nUpdating document...");
        var updatedDoc = new VectorDocument(
            id: "doc1",
            content: "The quick brown fox jumps over the lazy dog - UPDATED",
            metadata: new Dictionary<string, object>
            {
                ["category"] = "animals",
                ["year"] = 2024,
                ["updated"] = true
            }
        );
        await faissDb.UpdateDocumentsAsync(new[] { updatedDoc });

        var updated = await faissDb.GetDocumentsAsync(new[] { "doc1" });
        Console.WriteLine($"Updated: {updated[0].Content}");

        // Delete a document
        Console.WriteLine("\nDeleting document...");
        await faissDb.DeleteDocumentsAsync(new[] { "doc3" });

        var remaining = await faissDb.GetDocumentsAsync(new[] { "doc1", "doc2", "doc3" });
        Console.WriteLine($"Remaining documents: {remaining.Length}");

        // Clean up
        Console.WriteLine("\nCleaning up...");
        await faissDb.DeleteCollectionAsync("example_collection");

        Console.WriteLine("Example completed successfully!");
    }

    private static float[] GenerateRandomEmbedding(int dimension)
    {
        var random = new Random();
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random values between -1 and 1
        }
        // Normalize the embedding
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= (float)magnitude;
        }
        return embedding;
    }
}
