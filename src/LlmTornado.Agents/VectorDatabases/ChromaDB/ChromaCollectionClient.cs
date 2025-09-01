using LlmTornado.Agents.VectorDatabases.ChromaDB.Common;
using LlmTornado.Agents.VectorDatabases.ChromaDB.Client.Models;
using LlmTornado.Agents.VectorDatabases.ChromaDB.Client.Models.Requests;
using LlmTornado.Agents.VectorDatabases.ChromaDB.Client.Models.Responses;

namespace LlmTornado.Agents.VectorDatabases.ChromaDB.Client;

public class ChromaCollectionClient
{
	private readonly ChromaCollection _collection;
	private readonly HttpClient _httpClient;

	public ChromaCollectionClient(ChromaCollection collection, ChromaConfigurationOptions options, HttpClient httpClient)
	{
		_collection = collection;
		_httpClient = httpClient;

		if (_httpClient.BaseAddress != options.Uri)
		{
			_httpClient.BaseAddress = options.Uri;
		}
	}

	public ChromaCollection Collection => _collection;

	public async Task<ChromaCollectionEntry?> Get(string id, ChromaWhereOperator? where = null, ChromaWhereDocumentOperator? whereDocument = null, ChromaGetInclude? include = null)
		=> (await Get([id], where: where, whereDocument: whereDocument, include: include)).FirstOrDefault();

	public async Task<List<ChromaCollectionEntry>> Get(List<string>? ids = null, ChromaWhereOperator? where = null, ChromaWhereDocumentOperator? whereDocument = null, int? limit = null, int? offset = null, ChromaGetInclude? include = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionGetRequest()
		{
			Ids = ids,
			Where = where?.ToWhere(),
			WhereDocument = whereDocument?.ToWhereDocument(),
			Limit = limit,
			Offset = offset,
			Include = (include ?? ChromaGetInclude.Metadatas | ChromaGetInclude.Documents).ToInclude(),
		};
		var response = await _httpClient.Post<CollectionGetRequest, CollectionEntriesGetResponse>("collections/{collection_id}/get", request, requestParams);
		return response.Map() ?? [];
	}

	public async Task<List<ChromaCollectionQueryEntry>> Query(ReadOnlyMemory<float> queryEmbeddings, int nResults = 10, ChromaWhereOperator? where = null, ChromaWhereDocumentOperator? whereDocument = null, ChromaQueryInclude? include = null)
		=> (await Query([queryEmbeddings], nResults: nResults, where: where, whereDocument: whereDocument, include: include)).FirstOrDefault() ?? [];

	public async Task<List<List<ChromaCollectionQueryEntry>>> Query(List<ReadOnlyMemory<float>> queryEmbeddings, int nResults = 10, ChromaWhereOperator? where = null, ChromaWhereDocumentOperator? whereDocument = null, ChromaQueryInclude? include = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionQueryRequest()
		{
			QueryEmbeddings = queryEmbeddings,
			NResults = nResults,
			Where = where?.ToWhere(),
			WhereDocument = whereDocument?.ToWhereDocument(),
			Include = (include ?? ChromaQueryInclude.Metadatas | ChromaQueryInclude.Documents | ChromaQueryInclude.Distances).ToInclude(),
		};
		var response = await _httpClient.Post<CollectionQueryRequest, CollectionEntriesQueryResponse>("collections/{collection_id}/query", request, requestParams);
		return response.Map() ?? [];
	}

	public async Task Add(List<string> ids, List<ReadOnlyMemory<float>>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionAddRequest()
		{
			Ids = ids,
			Embeddings = embeddings,
			Metadatas = metadatas,
			Documents = documents,
		};
		await _httpClient.Post("collections/{collection_id}/add", request, requestParams);
	}

	public async Task Update(List<string> ids, List<ReadOnlyMemory<float>>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionUpdateRequest()
		{
			Ids = ids,
			Embeddings = embeddings,
			Metadatas = metadatas,
			Documents = documents,
		};
		await _httpClient.Post("collections/{collection_id}/update", request, requestParams);
	}

	public async Task Upsert(List<string> ids, List<ReadOnlyMemory<float>>? embeddings = null, List<Dictionary<string, object>>? metadatas = null, List<string>? documents = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionUpsertRequest()
		{
			Ids = ids,
			Embeddings = embeddings,
			Metadatas = metadatas,
			Documents = documents,
		};
		await _httpClient.Post("collections/{collection_id}/upsert", request, requestParams);
	}

	public async Task Delete(List<string> ids, ChromaWhereOperator? where = null, ChromaWhereDocumentOperator? whereDocument = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionDeleteRequest()
		{
			Ids = ids,
			Where = where?.ToWhere(),
			WhereDocument = whereDocument?.ToWhereDocument(),
		};
		await _httpClient.Post("collections/{collection_id}/delete", request, requestParams);
	}

	public async Task<int> Count()
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		return await _httpClient.Get<int>("collections/{collection_id}/count", requestParams);
	}

	public async Task<List<ChromaCollectionEntry>> Peek(int limit = 10)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionPeekRequest()
		{
			Limit = limit,
		};
		var response = await _httpClient.Post<CollectionPeekRequest, CollectionEntriesGetResponse>("collections/{collection_id}/get", request, requestParams);
		return response.Map() ?? [];
	}

	public async Task Modify(string? name = null, Dictionary<string, object>? metadata = null)
	{
		var requestParams = new RequestQueryParams()
			.Insert("{collection_id}", _collection.Id);
		var request = new CollectionModifyRequest()
		{
			Name = name,
			Metadata = metadata,
		};
		await _httpClient.Put("collections/{collection_id}", request, requestParams);
	}
}
