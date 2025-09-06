using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class CollectionQueryRequest
{
	[JsonPropertyName("query_embeddings")]
	public required List<ReadOnlyMemory<float>> QueryEmbeddings { get; init; }

	[JsonPropertyName("n_results")]
	public int NResults { get; init; } = 10;

	[JsonPropertyName("where")]
	public Dictionary<string, object>? Where { get; init; }

	[JsonPropertyName("where_document")]
	public Dictionary<string, object>? WhereDocument { get; init; }

	[JsonPropertyName("include")]
	public required List<string> Include { get; init; }
}

internal class CollectionQueryRequestV2
{
    [JsonPropertyName("data")]
    public CollectionQueryRequest Data { get; init; }
}