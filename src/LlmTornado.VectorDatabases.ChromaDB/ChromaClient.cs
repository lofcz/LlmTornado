using LlmTornado.VectorDatabases.ChromaDB.Common;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;

namespace LlmTornado.VectorDatabases.ChromaDB.Client;

public class ChromaClient
{
	private readonly HttpClient _httpClient;
	private readonly ChromaTenant _currentTenant;
	private readonly ChromaDatabase _currentDatabase;

	public ChromaClient(ChromaConfigurationOptions options, HttpClient httpClient)
	{
		_httpClient = httpClient;
		_currentTenant = !string.IsNullOrEmpty(options.Tenant)
			? new ChromaTenant(options.Tenant)
			: ClientConstants.DefaultTenant;
		_currentDatabase = !string.IsNullOrEmpty(options.Database)
			? new ChromaDatabase(options.Database)
			: ClientConstants.DefaultDatabase;

		if (_httpClient.BaseAddress != options.Uri)
		{
			_httpClient.BaseAddress = options.Uri;
		}
		if (!string.IsNullOrEmpty(options.ChromaToken))
		{
			_httpClient.DefaultRequestHeaders.Remove(ClientConstants.ChromaTokenHeader);
			_httpClient.DefaultRequestHeaders.Add(ClientConstants.ChromaTokenHeader, options.ChromaToken);
		}
	}

	public async Task<List<ChromaCollection>> ListCollections(string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		return await _httpClient.Get<List<ChromaCollection>>("collections?tenant={tenant}&database={database}", requestParams);
	}

	public async Task<ChromaCollection> GetCollection(string name, string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{collectionName}", name)
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		return await _httpClient.Get<ChromaCollection>("collections/{collectionName}?tenant={tenant}&database={database}", requestParams);
	}

	public async Task<ChromaHeartbeat> Heartbeat()
	{
		return await _httpClient.Get<ChromaHeartbeat>("", new RequestQueryParams());
	}

	public async Task<ChromaCollection> CreateCollection(string name, Dictionary<string, object>? metadata = null, string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		var request = new CreateCollectionRequest()
		{
			Name = name,
			Metadata = metadata
		};
		return await _httpClient.Post<CreateCollectionRequest, ChromaCollection>("collections?tenant={tenant}&database={database}", request, requestParams);
	}

	public async Task<ChromaCollection> GetOrCreateCollection(string name, Dictionary<string, object>? metadata = null, string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		var request = new GetOrCreateCollectionRequest()
		{
			Name = name,
			Metadata = metadata
		};
		return await _httpClient.Post<GetOrCreateCollectionRequest, ChromaCollection>("collections?tenant={tenant}&database={database}", request, requestParams);
	}

	public async Task DeleteCollection(string name, string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{collectionName}", name)
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		await _httpClient.Delete("collections/{collectionName}?tenant={tenant}&database={database}", requestParams);
	}

	public async Task<string> GetVersion()
	{
		return await _httpClient.Get<string>("version", new RequestQueryParams());
	}

	public async Task<bool> Reset()
	{
		return await _httpClient.Post<ResetRequest, bool>("reset", null, new RequestQueryParams());
	}

	public async Task<int> CountCollections(string? tenant = null, string? database = null)
	{
		tenant = !string.IsNullOrEmpty(tenant) ? tenant : _currentTenant.Name;
		database = !string.IsNullOrEmpty(database) ? database : _currentDatabase.Name;
		var requestParams = new RequestQueryParams()
			.Insert("{tenant}", tenant)
			.Insert("{database}", database);
		return await _httpClient.Get<int>("count_collections?tenant={tenant}&database={database}", requestParams);
	}
}
