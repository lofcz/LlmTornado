namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

[Flags]
public enum ChromaGetInclude
{
	None = 0,
	Embeddings = 1 << 0,
	Metadatas = 1 << 1,
	Documents = 1 << 2,
}

internal static class ChromaGetIncludeExt
{
	public static List<string> ToInclude(this ChromaGetInclude include)
	{
		var result = new List<string>();
		if (include.HasFlag(ChromaGetInclude.Embeddings))
		{
			result.Add("embeddings");
		}
		if (include.HasFlag(ChromaGetInclude.Metadatas))
		{
			result.Add("metadatas");
		}
		if (include.HasFlag(ChromaGetInclude.Documents))
		{
			result.Add("documents");
		}
		return result;
	}
}
