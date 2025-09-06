using System.Text.Json.Serialization;

namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models;

public class ChromaHeartbeat
{
	[JsonPropertyName("nanosecond heartbeat")]
	public long NanosecondHeartbeat { get; set; }
}
