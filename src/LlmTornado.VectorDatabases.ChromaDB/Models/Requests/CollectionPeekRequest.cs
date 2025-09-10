using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class CollectionPeekRequest
{
	[JsonPropertyName("limit")]
	public int Limit { get; init; }
}
