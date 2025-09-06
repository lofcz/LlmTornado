using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models;

public class ChromaCollection
{
	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("name")]
	public string Name { get; }

	[JsonPropertyName("metadata")]
	public Dictionary<string, object>? Metadata { get; init; }

	[JsonPropertyName("tenant")]
	public string? Tenant { get; init; }

	[JsonPropertyName("database")]
	public string? Database { get; init; }

	public ChromaCollection(string name)
	{
		Name = name;
	}
}
