using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

internal class GeneralError
{
	[JsonPropertyName("error")]
	public string? Error { get; init; }
}
