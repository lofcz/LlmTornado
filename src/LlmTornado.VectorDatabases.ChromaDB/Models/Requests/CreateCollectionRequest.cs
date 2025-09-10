namespace LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

internal class CreateCollectionRequest : GetOrCreateCollectionRequestBase
{
	protected override bool GetOrCreate { get; } = false;
}
