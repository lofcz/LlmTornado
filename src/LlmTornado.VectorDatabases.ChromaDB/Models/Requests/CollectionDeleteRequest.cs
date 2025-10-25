using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class CollectionDeleteRequest
{
	[JsonPropertyName("ids")]
	public List<string>? Ids { get; init; }

	[JsonPropertyName("where")]
	public Dictionary<string, object>? Where { get; init; }

	[JsonPropertyName("where_document")]
	public Dictionary<string, object>? WhereDocument { get; init; }
}
