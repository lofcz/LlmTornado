using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

internal class CollectionEntriesGetResponse
{
	[JsonPropertyName("ids")]
	public required List<string> Ids { get; init; }

	[JsonPropertyName("embeddings")]
	public required List<ReadOnlyMemory<float>?> Embeddings { get; init; }

	[JsonPropertyName("metadatas")]
	public required List<Dictionary<string, object>?> Metadatas { get; init; }

	[JsonPropertyName("documents")]
	public required List<string?> Documents { get; init; }

	[JsonPropertyName("uris")]
	public required List<List<string?>?> Uris { get; init; }

	[JsonPropertyName("data")]
	public required dynamic? Data { get; init; }
}
