using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

public interface IWhereConvertible
{
    Dictionary<string, object> ToWhere();
}

public abstract class TornadoWhereOperator : IWhereConvertible
{
    protected string Operator { get; }

    protected TornadoWhereOperator(string @operator)
    {
        Operator = @operator;
    }

    public abstract Dictionary<string, object> ToWhere();

    public static TornadoWhereOperator In(string key, params object[] values)
        => new TornadoWhereValueOperator(key, "$in", values);

    public static TornadoWhereOperator NotIn(string key, params object[] values)
        => new TornadoWhereValueOperator(key, "$nin", values);

    public static TornadoWhereOperator GreaterThan(string key, object value)
        => new TornadoWhereValueOperator(key, "$gt", value);

    public static TornadoWhereOperator GreaterThanOrEqual(string key, object value)
        => new TornadoWhereValueOperator(key, "$gte", value);

    public static TornadoWhereOperator LessThan(string key, object value)
        => new TornadoWhereValueOperator(key, "$lt", value);

    public static TornadoWhereOperator LessThanOrEqual(string key, object value)
        => new TornadoWhereValueOperator(key, "$lte", value);

    public static TornadoWhereOperator Equal(string key, object value)
        => new TornadoWhereValueOperator(key, "$eq", value);

    public static TornadoWhereOperator NotEqual(string key, object value)
        => new TornadoWhereValueOperator(key, "$ne", value);

    public static bool operator true(TornadoWhereOperator _)
        => false;
    public static bool operator false(TornadoWhereOperator _)
        => false;

    public static TornadoWhereOperator operator &(TornadoWhereOperator lhs, TornadoWhereOperator rhs)
        => new TornadoWhereLogicalOperator("$and", lhs, rhs);

    public static TornadoWhereOperator operator |(TornadoWhereOperator lhs, TornadoWhereOperator rhs)
        => new TornadoWhereLogicalOperator("$or", lhs, rhs);
}

internal class TornadoWhereLogicalOperator : TornadoWhereOperator
{
    protected TornadoWhereOperator Lhs { get; }
    protected TornadoWhereOperator Rhs { get; }

    internal TornadoWhereLogicalOperator(string @operator, TornadoWhereOperator lhs, TornadoWhereOperator rhs)
        : base(@operator)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    public override Dictionary<string, object> ToWhere()
        => new()
        {
        { Operator, new object[] { Lhs.ToWhere(), Rhs.ToWhere() } }
        };
}

internal class TornadoWhereValueOperator : TornadoWhereOperator
{
    protected string Key { get; }
    protected object Value { get; }

    internal TornadoWhereValueOperator(string key, string @operator, object value)
        : base(@operator)
    {
        Key = key;
        Value = value;
    }

    public override Dictionary<string, object> ToWhere()
        => new()
        {
        { Key, new Dictionary<string, object> { { Operator, Value } } }
        };
}
