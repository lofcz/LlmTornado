namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public abstract class ChromaWhere
{
	protected string Operator { get; }

	protected ChromaWhere(string @operator)
	{
		Operator = @operator;
	}

	internal abstract Dictionary<string, object> ToWhere();

	public static ChromaWhere In(string field, params object[] values)
		=> new ChromaWhereValue<object>(field, "$in", values);

	public static ChromaWhere NotIn(string field, params object[] values)
		=> new ChromaWhereValue<object>(field, "$nin", values);

	public static ChromaWhere GreaterThan(string field, object value)
		=> new ChromaWhereValue<object>(field, "$gt", value);

	public static ChromaWhere GreaterThanOrEqual(string field, object value)
		=> new ChromaWhereValue<object>(field, "$gte", value);

	public static ChromaWhere LessThan(string field, object value)
		=> new ChromaWhereValue<object>(field, "$lt", value);

	public static ChromaWhere LessThanOrEqual(string field, object value)
		=> new ChromaWhereValue<object>(field, "$lte", value);

	public static ChromaWhere Equal(string field, object value)
		=> new ChromaWhereValue<object>(field, "$eq", value);

	public static ChromaWhere NotEqual(string field, object value)
		=> new ChromaWhereValue<object>(field, "$ne", value);

	public static bool operator true(ChromaWhere _)
		=> false;
	public static bool operator false(ChromaWhere _)
		=> false;

	public static ChromaWhere operator &(ChromaWhere lhs, ChromaWhere rhs)
		=> new ChromaWhereLogical("$and", lhs, rhs);

	public static ChromaWhere operator |(ChromaWhere lhs, ChromaWhere rhs)
		=> new ChromaWhereLogical("$or", lhs, rhs);
}

internal class ChromaWhereLogical : ChromaWhere
{
	protected ChromaWhere Lhs { get; }
	protected ChromaWhere Rhs { get; }

	internal ChromaWhereLogical(string @operator, ChromaWhere lhs, ChromaWhere rhs)
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

internal class ChromaWhereValue<T> : ChromaWhere
{
	protected string Field { get; }
	protected T Value { get; }

	internal ChromaWhereValue(string field, string @operator, T value)
		: base(@operator)
	{
		Field = field;
		Value = value;
	}

	internal override Dictionary<string, object> ToWhere()
		=> new()
		{
			{ Field, new Dictionary<string, T> { { Operator, Value } } }
		};
}
