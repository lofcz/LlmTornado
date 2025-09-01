using LlmTornado.Agents.VectorDatabases.ChromaDB.Common;

namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public class ChromaConfigurationOptions
{
	public Uri Uri { get; init; }
	public string? Tenant { get; init; }
	public string? Database { get; init; }
	public string? ChromaToken { get; init; }

	public ChromaConfigurationOptions(Uri uri, string? defaultTenant = null, string? defaultDatabase = null, string? chromaToken = null)
	{
		Uri = uri;
		Tenant = defaultTenant;
		Database = defaultDatabase;
		ChromaToken = chromaToken;
	}

	public ChromaConfigurationOptions(string uri, string? defaultTenant = null, string? defaultDatabase = null, string? chromaToken = null)
		: this(new Uri(uri), defaultTenant, defaultDatabase, chromaToken)
	{ }

	public ChromaConfigurationOptions()
		: this(ClientConstants.DefaultUri)
	{ }

	public ChromaConfigurationOptions WithUri(Uri uri)
		=> new(uri, Tenant, Database, ChromaToken);

	public ChromaConfigurationOptions WithUri(string uri)
		=> new(uri, Tenant, Database, ChromaToken);

	public ChromaConfigurationOptions WithTenant(string tenant)
		=> new(Uri, tenant, Database, ChromaToken);

	public ChromaConfigurationOptions WithDatabase(string database)
		=> new(Uri, Tenant, database, ChromaToken);

	public ChromaConfigurationOptions WithChromaToken(string chromaToken)
		=> new(Uri, Tenant, Database, chromaToken);
}
