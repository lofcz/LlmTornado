namespace LlmTornado.VectorDatabases.ChromaDB.Client;

[Flags]
public enum ChromaQueryInclude
{
	None = 0,
	Embeddings = 1 << 0,
	Metadatas = 1 << 1,
	Documents = 1 << 2,
	Distances = 1 << 3,
}

internal static class ChromaQueryIncludeExt
{
	public static List<string> ToInclude(this ChromaQueryInclude include)
	{
		var result = new List<string>();
		if (include.HasFlag(ChromaQueryInclude.Embeddings))
		{
			result.Add("embeddings");
		}
		if (include.HasFlag(ChromaQueryInclude.Metadatas))
		{
			result.Add("metadatas");
		}
		if (include.HasFlag(ChromaQueryInclude.Documents))
		{
			result.Add("documents");
		}
		if (include.HasFlag(ChromaQueryInclude.Distances))
		{
			result.Add("distances");
		}
		return result;
	}
}
