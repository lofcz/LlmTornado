namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public abstract class ChromaWhereDocument
{
	protected string Operator { get; }

	protected ChromaWhereDocument(string @operator)
	{
		Operator = @operator;
	}

	internal abstract Dictionary<string, object> ToWhereDocument();

	public static ChromaWhereDocument Contains(char ch)
		=> Contains(ch.ToString());
	public static ChromaWhereDocument Contains(string str)
		=> new ChromaWhereDocumentStr("$contains", str);

	public static ChromaWhereDocument NotContains(char ch)
		=> NotContains(ch.ToString());
	public static ChromaWhereDocument NotContains(string str)
		=> new ChromaWhereDocumentStr("$not_contains", str);

	public static bool operator true(ChromaWhereDocument _)
		=> false;
	public static bool operator false(ChromaWhereDocument _)
		=> false;

	public static ChromaWhereDocument operator &(ChromaWhereDocument lhs, ChromaWhereDocument rhs)
		=> new ChromaWhereDocumentLogical("$and", lhs, rhs);

	public static ChromaWhereDocument operator |(ChromaWhereDocument lhs, ChromaWhereDocument rhs)
		=> new ChromaWhereDocumentLogical("$or", lhs, rhs);
}

internal class ChromaWhereDocumentLogical : ChromaWhereDocument
{
	protected ChromaWhereDocument Lhs { get; }
	protected ChromaWhereDocument Rhs { get; }

	internal ChromaWhereDocumentLogical(string @operator, ChromaWhereDocument lhs, ChromaWhereDocument rhs)
		: base(@operator)
	{
		Lhs = lhs;
		Rhs = rhs;
	}

	internal override Dictionary<string, object> ToWhereDocument()
		=> new()
		{
			{ Operator, new object[] { Lhs.ToWhereDocument(), Rhs.ToWhereDocument() } }
		};
}

internal class ChromaWhereDocumentStr : ChromaWhereDocument
{
	protected string Str { get; }

	internal ChromaWhereDocumentStr(string @operator, string str)
		: base(@operator)
	{
		Str = str;
	}

	internal override Dictionary<string, object> ToWhereDocument()
		=> new()
		{
			{ Operator, Str }
		};
}
