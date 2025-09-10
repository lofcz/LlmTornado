using LlmTornado.VectorDatabases.ChromaDB.Client.Models;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

namespace LlmTornado.VectorDatabases.ChromaDB.Common;

internal static class CollectionEntryMapper
{
	public static List<ChromaCollectionEntry> Map(this CollectionEntriesGetResponse response)
	{
		return response.Ids
			.Select((id, i) => new ChromaCollectionEntry(id)
			{
				Embeddings = response.Embeddings?[i],
				Metadata = response.Metadatas?[i],
				Document = response.Documents?[i],
				Uris = response.Uris?[i],
				Data = response.Data,
			})
			.ToList();
	}
}
