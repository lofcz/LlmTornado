namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public abstract class ChromaWhereDocumentOperator
{
	protected string Operator { get; }

	protected ChromaWhereDocumentOperator(string @operator)
	{
		Operator = @operator;
	}

	internal abstract Dictionary<string, object> ToWhereDocument();

	public static ChromaWhereDocumentOperator Contains(char value)
		=> Contains(value.ToString());
	public static ChromaWhereDocumentOperator Contains(string value)
		=> new ChromaWhereDocumentStringOperator("$contains", value);

	public static ChromaWhereDocumentOperator NotContains(char value)
		=> NotContains(value.ToString());
	public static ChromaWhereDocumentOperator NotContains(string value)
		=> new ChromaWhereDocumentStringOperator("$not_contains", value);

	public static bool operator true(ChromaWhereDocumentOperator _)
		=> false;
	public static bool operator false(ChromaWhereDocumentOperator _)
		=> false;

	public static ChromaWhereDocumentOperator operator &(ChromaWhereDocumentOperator lhs, ChromaWhereDocumentOperator rhs)
		=> new ChromaWhereDocumentLogicalOperator("$and", lhs, rhs);

	public static ChromaWhereDocumentOperator operator |(ChromaWhereDocumentOperator lhs, ChromaWhereDocumentOperator rhs)
		=> new ChromaWhereDocumentLogicalOperator("$or", lhs, rhs);
}

internal class ChromaWhereDocumentLogicalOperator : ChromaWhereDocumentOperator
{
	protected ChromaWhereDocumentOperator Lhs { get; }
	protected ChromaWhereDocumentOperator Rhs { get; }

	internal ChromaWhereDocumentLogicalOperator(string @operator, ChromaWhereDocumentOperator lhs, ChromaWhereDocumentOperator rhs)
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

internal class ChromaWhereDocumentStringOperator : ChromaWhereDocumentOperator
{
	protected string String { get; }

	internal ChromaWhereDocumentStringOperator(string @operator, string @string)
		: base(@operator)
	{
		String = @string;
	}

	internal override Dictionary<string, object> ToWhereDocument()
		=> new()
		{
			{ Operator, String }
		};
}
