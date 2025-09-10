namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models;

public class ChromaCollectionQueryEntry
{
	public string Id { get; }
	public float Distance { get; init; }
	public Dictionary<string, object>? Metadata { get; init; }
	public ReadOnlyMemory<float>? Embeddings { get; init; }
	public string? Document { get; init; }
	public List<string?>? Uris { get; init; }
	public dynamic? Data { get; init; }

	public ChromaCollectionQueryEntry(string id)
	{
		Id = id;
	}
}
