using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

internal class CollectionEntriesQueryResponse
{
	[JsonPropertyName("ids")]
	public required List<List<string>> Ids { get; init; }

	[JsonPropertyName("distances")]
	public required List<ReadOnlyMemory<float>> Distances { get; init; }

	[JsonPropertyName("metadatas")]
	public required List<List<Dictionary<string, object>>>? Metadatas { get; init; }

	[JsonPropertyName("embeddings")]
	public required List<List<ReadOnlyMemory<float>>>? Embeddings { get; init; }

	[JsonPropertyName("documents")]
	public required List<List<string?>>? Documents { get; init; }

	[JsonPropertyName("uris")]
	public required List<List<List<string?>>>? Uris { get; init; }

	[JsonPropertyName("data")]
	public required dynamic? Data { get; init; }
}
