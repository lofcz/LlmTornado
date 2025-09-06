using LlmTornado.VectorDatabases.ChromaDB.Client.Models;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

namespace LlmTornado.VectorDatabases.ChromaDB.Common;

internal static class CollectionQueryEntryMapper
{
	public static List<List<ChromaCollectionQueryEntry>> Map(this CollectionEntriesQueryResponse response)
	{
		return response.Ids
			.Select((_, i) => response.Ids[i]
				.Select((id, j) => new ChromaCollectionQueryEntry(id)
				{
					Distance = response.Distances[i].Span[j],
					Metadata = response.Metadatas?[i][j],
					Embeddings = response.Embeddings?[i][j],
					Document = response.Documents?[i][j],
					Uris = response.Uris?[i][j],
					Data = response.Data,
				})
				.ToList())
			.ToList();
	}
}
