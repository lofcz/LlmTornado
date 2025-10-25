# LlmTornado.VectorDatabases.Qdrant

Qdrant vector database integration for LlmTornado. This library provides a connector to Qdrant for vector storage and similarity search operations.

## Features

- Full implementation of `IVectorDatabase` interface
- Support for all CRUD operations on vector documents
- Similarity search with optional metadata filtering
- Async and sync method variants
- Built on the official Qdrant .NET client

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.Qdrant
```

## Usage

```csharp
using LlmTornado.VectorDatabases.Qdrant;
using LlmTornado.VectorDatabases;

// Initialize the Qdrant database connection
var qdrantDb = new QdrantVectorDatabase(
    host: "localhost",
    port: 6334,
    vectorDimension: 1536
);

// Initialize a collection
await qdrantDb.InitializeCollectionAsync("my_collection");

// Create documents with embeddings
var documents = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "Sample document",
        metadata: new Dictionary<string, object> { ["category"] = "sample" },
        embedding: new float[1536] // your embedding vector
    )
};

// Add documents
await qdrantDb.AddDocumentsAsync(documents);

// Query by embedding
var results = await qdrantDb.QueryByEmbeddingAsync(
    queryEmbedding,
    topK: 5,
    includeScore: true
);

// Get documents by ID
var docs = await qdrantDb.GetDocumentsAsync(new[] { "doc1", "doc2" });

// Update documents
await qdrantDb.UpdateDocumentsAsync(updatedDocuments);

// Delete documents
await qdrantDb.DeleteDocumentsAsync(new[] { "doc1" });

// Delete collection
await qdrantDb.DeleteCollectionAsync("my_collection");
```

## Configuration

The `QdrantVectorDatabase` constructor accepts the following parameters:

- `host`: Qdrant server host (default: "localhost")
- `port`: Qdrant server port (default: 6334 for gRPC)
- `vectorDimension`: Dimension of vectors to be stored (default: 1536)
- `https`: Whether to use HTTPS (default: false)
- `apiKey`: Optional API key for authentication

## Requirements

- .NET 8.0 or .NET Standard 2.0
- Qdrant server running and accessible
- Qdrant.Client NuGet package (automatically included)

## License

See the main LlmTornado repository for license information.
