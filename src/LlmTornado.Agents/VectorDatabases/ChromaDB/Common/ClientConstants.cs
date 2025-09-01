using LlmTornado.Agents.VectorDatabases.ChromaDB.Client.Models;

namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Common;

internal static class ClientConstants
{
	public const string DefaultTenantName = "default_tenant";
	public const string DefaultDatabaseName = "default_database";
	public const string DefaultUri = "http://localhost:8000/api/v1/";
	public const string ChromaTokenHeader = "X-Chroma-Token";

	public static ChromaTenant DefaultTenant { get; } = new(DefaultTenantName);
	public static ChromaDatabase DefaultDatabase { get; } = new(DefaultDatabaseName);
}
