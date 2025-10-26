namespace LlmTornado.VectorDatabases.Pinecone.Integrations;

/// <summary>
/// Adapter to convert TornadoWhereOperator to Pinecone metadata filter format.
/// </summary>
public class TornadoPineconeWhere
{
    public TornadoWhereOperator? TornadoWhereOperator { get; set; }

    public TornadoPineconeWhere(TornadoWhereOperator? where)
    {
        TornadoWhereOperator = where;
    }

    /// <summary>
    /// Converts TornadoWhereOperator to Pinecone filter dictionary.
    /// Pinecone uses a similar structure but may have different operator names.
    /// </summary>
    public Dictionary<string, object>? ToWhere()
    {
        return TornadoWhereOperator?.ToWhere();
    }
}

