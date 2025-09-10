using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models;

public class ChromaDatabase
{
	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("name")]
	public string Name { get; }

	[JsonPropertyName("tenant")]
	public string? Tenant { get; init; }

	public ChromaDatabase(string name)
	{
		Name = name;
	}
}
