using System.Text.Json.Serialization;

namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client.Models.Requests;

internal abstract class GetOrCreateCollectionRequestBase
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("metadata")]
	public Dictionary<string, object>? Metadata { get; init; }

	[JsonInclude]
	[JsonPropertyName("get_or_create")]
	protected abstract bool GetOrCreate { get; }
}
