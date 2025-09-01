namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public abstract class ChromaWhereOperator
{
	protected string Operator { get; }

	protected ChromaWhereOperator(string @operator)
	{
		Operator = @operator;
	}

	internal abstract Dictionary<string, object> ToWhere();

	public static ChromaWhereOperator In(string key, params object[] values)
		=> new ChromaWhereValueOperator(key, "$in", values);

	public static ChromaWhereOperator NotIn(string key, params object[] values)
		=> new ChromaWhereValueOperator(key, "$nin", values);

	public static ChromaWhereOperator GreaterThan(string key, object value)
		=> new ChromaWhereValueOperator(key, "$gt", value);

	public static ChromaWhereOperator GreaterThanOrEqual(string key, object value)
		=> new ChromaWhereValueOperator(key, "$gte", value);

	public static ChromaWhereOperator LessThan(string key, object value)
		=> new ChromaWhereValueOperator(key, "$lt", value);

	public static ChromaWhereOperator LessThanOrEqual(string key, object value)
		=> new ChromaWhereValueOperator(key, "$lte", value);

	public static ChromaWhereOperator Equal(string key, object value)
		=> new ChromaWhereValueOperator(key, "$eq", value);

	public static ChromaWhereOperator NotEqual(string key, object value)
		=> new ChromaWhereValueOperator(key, "$ne", value);

	public static bool operator true(ChromaWhereOperator _)
		=> false;
	public static bool operator false(ChromaWhereOperator _)
		=> false;

	public static ChromaWhereOperator operator &(ChromaWhereOperator lhs, ChromaWhereOperator rhs)
		=> new ChromaWhereLogicalOperator("$and", lhs, rhs);

	public static ChromaWhereOperator operator |(ChromaWhereOperator lhs, ChromaWhereOperator rhs)
		=> new ChromaWhereLogicalOperator("$or", lhs, rhs);
}

internal class ChromaWhereLogicalOperator : ChromaWhereOperator
{
	protected ChromaWhereOperator Lhs { get; }
	protected ChromaWhereOperator Rhs { get; }

	internal ChromaWhereLogicalOperator(string @operator, ChromaWhereOperator lhs, ChromaWhereOperator rhs)
		: base(@operator)
	{
		Lhs = lhs;
		Rhs = rhs;
	}

	internal override Dictionary<string, object> ToWhere()
		=> new()
		{
			{ Operator, new object[] { Lhs.ToWhere(), Rhs.ToWhere() } }
		};
}

internal class ChromaWhereValueOperator : ChromaWhereOperator
{
	protected string Key { get; }
	protected object Value { get; }

	internal ChromaWhereValueOperator(string key, string @operator, object value)
		: base(@operator)
	{
		Key = key;
		Value = value;
	}

	internal override Dictionary<string, object> ToWhere()
		=> new()
		{
			{ Key, new Dictionary<string, object> { { Operator, Value } } }
		};
}
