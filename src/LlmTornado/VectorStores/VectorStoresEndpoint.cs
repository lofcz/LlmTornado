using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.VectorStores;

/// <summary>
///     This endpoint classifies text against the OpenAI Content Policy
/// </summary>
public class VectorStoresEndpoint : EndpointBase
{
    /// <summary>
    ///     Constructor of the api endpoint. Rather than instantiating this yourself, access it through an instance of
    ///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Moderation" />.
    /// </summary>
    /// <param name="api"></param>
    public VectorStoresEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "completions".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.VectorStores;

    /// <summary>
    ///		Create a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<VectorStore>> Create(CreateVectorStoreRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: request, ct: cancellationToken);
    }

    /// <summary>
    ///		Retrieves a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<VectorStore>> Retrieve(string vectorStoreId, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), ct:cancellationToken);
    }

    /// <summary>
    ///     Retrieves a list of vector stores. Available only for OpenAI
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<ListResponse<VectorStore>>> List(ListQuery? query = null, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<VectorStore>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi)), query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }
    
    /// <summary>
    ///     Delete a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<HttpCallResult<bool>> Delete(string vectorStoreId, CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, HttpVerbs.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), ct: cancellationToken).ConfigureAwait(false);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, status.Request);
    }
    
    /// <summary>
    ///     Modifies vector store. All fields in the existing vector store are replaced with the fields from
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="vectorStoreId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="VectorStore" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="VectorStore" />.</returns>
    public Task<HttpCallResult<VectorStore>> Modify(string vectorStoreId, VectorStoreModifyRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), request, ct: cancellationToken);
    }

    /// <summary>
    ///     Retrieves a list of vector stores. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<ListResponse<VectorStoreFile>>> ListFiles(string vectorStoreId, ListQuery? query = null, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<VectorStoreFile>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/files"), query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    ///  <summary>
    /// 		Retrieves a vector store file. Available only for OpenAI
    ///  </summary>
    ///  <param name="vectorStoreId"></param>
    ///  <param name="fileId"></param>
    ///  <param name="cancellationToken"></param>
    ///  <returns></returns>
    public Task<HttpCallResult<VectorStoreFile>> RetrieveFiles(string vectorStoreId, string fileId, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<VectorStoreFile>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/files/{fileId}"), ct: cancellationToken);
    }
    
    /// <summary>
    ///     Modifies vector store file.
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="vectorStoreId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="VectorStore" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="VectorStore" />.</returns>
    public Task<HttpCallResult<VectorStoreFile>> CreateFile(string vectorStoreId, CreateVectorStoreFileRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStoreFile>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/files"), request, ct: cancellationToken);
    }

    /// <summary>
    ///     Delete a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<HttpCallResult<bool>> DeleteFile(string vectorStoreId, string fileId, CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, HttpVerbs.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/files/{fileId}"), ct: cancellationToken).ConfigureAwait(false);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, status.Request);
    }

    /// <summary>
    ///     Retrieves a list of files in vector store batch. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="batchId"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<ListResponse<VectorStoreFile>>> ListBatchFiles(string vectorStoreId, string batchId, ListQuery? query = null, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<VectorStoreFile>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/file_batches/{batchId}/files"), query?.ToQueryParams(LLmProviders.OpenAi), cancellationToken);
    }

    ///  <summary>
    /// 		Retrieves a vector store file. Available only for OpenAI
    ///  </summary>
    ///  <param name="vectorStoreId"></param>
    ///  <param name="batchId"></param>
    ///  <param name="cancellationToken"></param>
    ///  <returns></returns>
    public Task<HttpCallResult<VectorStoreFileBatch>> RetrieveBatchFile(string vectorStoreId, string batchId, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<VectorStoreFileBatch>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/file_batches/{batchId}"), ct:cancellationToken);
    }
    
    /// <summary>
    ///     Modifies vector store file.
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="vectorStoreId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="VectorStore" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="VectorStore" />.</returns>
    public Task<HttpCallResult<VectorStoreFileBatch>> CreateBatchFile(string vectorStoreId, CreateVectorStoreFileBatchRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStoreFileBatch>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/file_batches"), request, ct: cancellationToken);
    }

    /// <summary>
    ///     Delete a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="batchId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<VectorStoreFileBatch>> CancelFileBatch(string vectorStoreId, string batchId, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStoreFileBatch>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/file_batches/{batchId}/cancel"), ct: cancellationToken);
    }
}