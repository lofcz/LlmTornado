namespace LlmTornado.VectorDatabases.PgVector;

public class PgVectorEntry
{
    public string Id { get; set; }
    public string? Document { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public float[]? Embedding { get; set; }
    public float? Distance { get; set; }

    public PgVectorEntry(string id, string? document = null, Dictionary<string, object>? metadata = null, float[]? embedding = null, float? distance = null)
    {
        Id = id;
        Document = document;
        Metadata = metadata;
        Embedding = embedding;
        Distance = distance;
    }
}
