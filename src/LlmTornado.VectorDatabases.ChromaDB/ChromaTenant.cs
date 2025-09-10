using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models;

public class ChromaTenant
{
	[JsonPropertyName("name")]
	public string Name { get; }

	public ChromaTenant(string name)
	{
		Name = name;
	}
}
