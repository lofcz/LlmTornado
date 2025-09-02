using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class CollectionUpdateRequest
{
	[JsonPropertyName("ids")]
	public required List<string> Ids { get; init; }

	[JsonPropertyName("embeddings")]
	public List<ReadOnlyMemory<float>>? Embeddings { get; init; }

	[JsonPropertyName("metadatas")]
	public List<Dictionary<string, object>>? Metadatas { get; init; }

	[JsonPropertyName("documents")]
	public List<string>? Documents { get; init; }
}
