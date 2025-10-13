# LlmTornado.VectorDatabases.PgVector

A full-featured PostgreSQL pgvector implementation for LlmTornado, providing vector database capabilities with collection management, CRUD operations, vector similarity search, and JSON metadata filtering.

## Features

- **Collection Management**: Create, get, list, and delete vector collections
- **CRUD Operations**: Full Create, Read, Update, Delete, and Upsert support for vector documents
- **Vector Similarity Search**: Efficient cosine similarity search using pgvector extension
- **Metadata Filtering**: Advanced JSON-based metadata filtering using PostgreSQL JSONB operators
- **Transaction Support**: Safe batch operations with transaction rollback on errors
- **Automatic Indexing**: Automatic creation of vector and metadata indexes for optimal performance
- **No HTTP Dependency**: Direct database access without requiring HTTP API layer

## Prerequisites

- PostgreSQL 12 or higher
- pgvector extension installed in your PostgreSQL database
- .NET 8.0 or .NET Standard 2.0

## Installation

```bash
dotnet add package LlmTornado.VectorDatabases.PgVector
```

## Setup

### Install pgvector Extension

First, ensure the pgvector extension is installed in your PostgreSQL database:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

## Usage

### Basic Setup

```csharp
using LlmTornado.VectorDatabases.PgVector.Integrations;

// Initialize with connection string and vector dimension
var connectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";
var pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

// Initialize a collection
await pgVector.InitializeCollection("my_collection");
```

### Adding Documents

```csharp
var documents = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "This is a document about AI",
        embedding: new float[] { /* 1536 dimensional vector */ },
        metadata: new Dictionary<string, object>
        {
            { "category", "technology" },
            { "year", 2024 }
        }
    ),
    new VectorDocument(
        id: "doc2",
        content: "Another document about machine learning",
        embedding: new float[] { /* 1536 dimensional vector */ },
        metadata: new Dictionary<string, object>
        {
            { "category", "AI" },
            { "year", 2024 }
        }
    )
};

await pgVector.AddDocumentsAsync(documents);
```

### Querying by Embedding

```csharp
// Simple query
var queryEmbedding = new float[] { /* 1536 dimensional query vector */ };
var results = await pgVector.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    topK: 5
);

// Query with metadata filtering
var results = await pgVector.QueryByEmbeddingAsync(
    embedding: queryEmbedding,
    where: TornadoWhereOperator.Equal("category", "technology"),
    topK: 5
);
```

### Metadata Filtering

The implementation supports complex metadata filtering using `TornadoWhereOperator`:

```csharp
// Equal
var where = TornadoWhereOperator.Equal("category", "technology");

// Greater than
var where = TornadoWhereOperator.GreaterThan("year", 2020);

// In array
var where = TornadoWhereOperator.In("category", "AI", "ML", "technology");

// Complex conditions with AND
var where = TornadoWhereOperator.Equal("category", "technology") 
          & TornadoWhereOperator.GreaterThan("year", 2020);

// Complex conditions with OR
var where = TornadoWhereOperator.Equal("category", "AI") 
          | TornadoWhereOperator.Equal("category", "ML");
```

### Updating Documents

```csharp
var updatedDocuments = new[]
{
    new VectorDocument(
        id: "doc1",
        content: "Updated content",
        metadata: new Dictionary<string, object> { { "updated", true } }
    )
};

await pgVector.UpdateDocumentsAsync(updatedDocuments);
```

### Upserting Documents

```csharp
// Upsert will insert if not exists, update if exists
await pgVector.UpsertDocumentsAsync(documents);
```

### Getting Documents by ID

```csharp
var ids = new[] { "doc1", "doc2" };
var documents = await pgVector.GetDocumentsAsync(ids);
```

### Deleting Documents

```csharp
var ids = new[] { "doc1", "doc2" };
await pgVector.DeleteDocumentsAsync(ids);
```

### Deleting Collections

```csharp
await pgVector.DeleteCollectionAsync("my_collection");
```

## Architecture

### Core Components

- **PgVectorClient**: Manages PostgreSQL connections and collection-level operations
- **PgVectorCollectionClient**: Handles document CRUD operations and vector queries within a collection
- **PgVectorCollection**: Represents collection metadata
- **TornadoPgVector**: Implements IVectorDatabase interface for seamless integration with LlmTornado

### Database Schema

Each collection creates a table with the following structure:

```sql
CREATE TABLE <collection_name>_vectors (
    id TEXT PRIMARY KEY,
    document TEXT,
    metadata JSONB,
    embedding vector(<dimension>)
);
```

Two indexes are automatically created:
1. **IVFFlat index** on embeddings for fast similarity search
2. **GIN index** on metadata JSONB for efficient metadata queries

## Performance Considerations

- The implementation uses cosine distance operator (`<=>`) for similarity search
- IVFFlat indexes provide approximate nearest neighbor search with good performance/accuracy tradeoff
- Batch operations use transactions to ensure data consistency
- Connection pooling is handled by Npgsql automatically

## Configuration Options

### Connection String

Standard PostgreSQL connection string format:

```csharp
var connectionString = "Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass;Pooling=true;MinPoolSize=1;MaxPoolSize=20";
```

### Vector Dimension

Specify the dimension of your embeddings when creating the TornadoPgVector instance:

```csharp
// For OpenAI text-embedding-3-small (default)
var pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536);

// For other embedding models, adjust accordingly
var pgVector = new TornadoPgVector(connectionString, vectorDimension: 768);
```

### Schema

Optionally specify a PostgreSQL schema (defaults to "public"):

```csharp
var pgVector = new TornadoPgVector(connectionString, vectorDimension: 1536, schema: "my_schema");
```

## Error Handling

The implementation includes proper error handling with transaction rollback:

```csharp
try
{
    await pgVector.AddDocumentsAsync(documents);
}
catch (InvalidOperationException ex)
{
    // Collection not initialized
    Console.WriteLine($"Error: {ex.Message}");
}
catch (NpgsqlException ex)
{
    // Database connection or query error
    Console.WriteLine($"Database error: {ex.Message}");
}
```

## Comparison with ChromaDB Implementation

| Feature | PgVector | ChromaDB |
|---------|----------|----------|
| API Type | Direct DB | HTTP API |
| Setup | Requires PostgreSQL + pgvector | Requires ChromaDB server |
| Performance | Low latency (direct DB) | Network overhead |
| Metadata Filtering | Native JSONB operators | HTTP query parameters |
| Transactions | Full support | Limited |
| Scalability | PostgreSQL native | ChromaDB specific |
| Production Ready | Yes | Yes |

## License

This project follows the LlmTornado licensing terms.

## Contributing

Contributions are welcome! Please ensure all tests pass and follow the existing code style.
