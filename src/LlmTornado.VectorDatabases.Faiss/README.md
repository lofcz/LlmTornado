# LlmTornado.VectorDatabases.Faiss

A FAISS vector database connector for LlmTornado, providing efficient similarity search and clustering of dense vectors.

## Overview

This package implements the `IVectorDatabase` interface using Facebook's FAISS (Facebook AI Similarity Search) library through the FaissNet wrapper. FAISS is a library for efficient similarity search and clustering of dense vectors, optimized for both CPU and GPU usage.

## Features

- **Efficient Vector Search**: Leverages FAISS's optimized algorithms for fast similarity search
- **Local Storage**: Stores indexes on the local filesystem
- **Metadata Support**: Store and filter documents using metadata
- **Full IVectorDatabase Implementation**: Supports all standard operations including Add, Get, Update, Upsert, Delete, and Query
- **Collection Management**: Create, delete, and manage multiple vector collections
- **Persistent Storage**: Indexes are saved to disk and reloaded on initialization

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.Faiss
```

## Usage

### Basic Setup

```csharp
using LlmTornado.VectorDatabases.Faiss.Integrations;

// Create a FAISS vector database instance
var faissDb = new FaissVectorDatabase(
    indexDirectory: "./my_faiss_indexes",  // Optional, defaults to "./faiss_indexes"
    vectorDimension: 1536                   // Optional, defaults to 1536
);

// Initialize a collection
await faissDb.InitializeCollection("my_collection");
```

### Adding Documents

```csharp
var documents = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "This is a sample document",
        embedding: new float[] { /* 1536-dimensional vector */ },
        metadata: new Dictionary<string, object> { ["category"] = "sample" }
    )
};

await faissDb.AddDocumentsAsync(documents);
```

### Querying by Embedding

```csharp
float[] queryEmbedding = /* your query vector */;

var results = await faissDb.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    topK: 5,
    includeScore: true
);

foreach (var doc in results)
{
    Console.WriteLine($"ID: {doc.Id}, Score: {doc.Score}, Content: {doc.Content}");
}
```

### Filtering with Metadata

```csharp
var whereFilter = TornadoWhereOperator.Equal("category", "sample");

var filteredResults = await faissDb.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    where: whereFilter,
    topK: 5
);
```

### Updating and Deleting Documents

```csharp
// Update documents
var updatedDoc = new VectorDocument(
    id: "doc1",
    content: "Updated content",
    metadata: new Dictionary<string, object> { ["category"] = "updated" }
);
await faissDb.UpdateDocumentsAsync(new[] { updatedDoc });

// Delete documents
await faissDb.DeleteDocumentsAsync(new[] { "doc1" });
```

## Architecture

The FAISS integration consists of several key components:

- **FaissVectorDatabase**: Main class implementing `IVectorDatabase`
- **FaissClient**: Manages collections and index files
- **FaissCollectionClient**: Handles operations on individual collections
- **FaissCollection**: Represents a collection with its metadata
- **FaissEntry**: Represents a single vector entry with metadata

## Storage

- Indexes are stored in the directory specified during initialization
- Each collection has two files:
  - `{collection_name}.index` - FAISS index file
  - `{collection_name}.metadata.json` - Document metadata and mappings
- Collection metadata is stored in `collections.json`

## Limitations

- FAISS IndexIDMap doesn't support in-place vector updates. To update embeddings, use `UpsertDocuments` which will delete and re-add the document.
- Delete operations require rebuilding the index
- Currently uses IndexFlatL2 (exact search). Future versions may support approximate search indexes (IVF, HNSW, etc.)

## Dependencies

- FaissNet 1.1.0 - .NET wrapper for FAISS library
- LlmTornado.VectorDatabases - Core vector database abstractions

## License

This package follows the same license as LlmTornado.

## Support

For issues, questions, or contributions, please visit the [LlmTornado GitHub repository](https://github.com/lofcz/LlmTornado).
