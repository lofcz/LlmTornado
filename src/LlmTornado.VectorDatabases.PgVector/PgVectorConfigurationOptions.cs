namespace LlmTornado.VectorDatabases.PgVector;

public class PgVectorConfigurationOptions
{
    public string ConnectionString { get; set; }
    public string? Schema { get; set; }

    public PgVectorConfigurationOptions(string connectionString, string? schema = null)
    {
        ConnectionString = connectionString;
        Schema = schema ?? "public";
    }
}
