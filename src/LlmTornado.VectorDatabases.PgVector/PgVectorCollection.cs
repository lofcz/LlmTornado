namespace LlmTornado.VectorDatabases.PgVector;

public class PgVectorCollection
{
    public string Name { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public int VectorDimension { get; set; }
    public SimilarityMetric Metric { get; set; } = SimilarityMetric.DotProduct;

    public PgVectorCollection(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        Name = name;
        VectorDimension = vectorDimension;
        Metadata = metadata;
    }
}
