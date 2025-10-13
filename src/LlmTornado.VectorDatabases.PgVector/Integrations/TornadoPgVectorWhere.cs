namespace LlmTornado.VectorDatabases.PgVector.Integrations;

public class TornadoPgVectorWhere
{
    public TornadoWhereOperator? TornadoWhereOperator { get; set; }
    
    public TornadoPgVectorWhere(TornadoWhereOperator where)
    {
        TornadoWhereOperator = where;
    }

    internal Dictionary<string, object>? ToWhere()
    {
        return TornadoWhereOperator?.ToWhere();
    }
}
