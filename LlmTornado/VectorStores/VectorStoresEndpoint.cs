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
    public Task<HttpCallResult<VectorStore>> CreateVectorStoreAsync(CreateVectorStoreRequest request,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: request,
            ct: cancellationToken);
    }

    /// <summary>
    ///		Retrieves a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<VectorStore>> RetrieveVectorStoreAsync(string vectorStoreId,
        CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), cancellationToken);
    }

    /// <summary>
    ///     Retrieves a list of vector stores. Available only for OpenAI
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<ListResponse<VectorStore>>> ListVectorStoresAsync(ListQuery? query = null,
        CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<VectorStore>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi)), cancellationToken);
    }
    
    
    /// <summary>
    ///     Delete a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="vectorStoreId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<HttpCallResult<bool>> DeleteVectorStoreAsync(string vectorStoreId,
        CancellationToken? cancellationToken = null)
    {
        HttpCallResult<DeletionStatus> status = await HttpAtomic<DeletionStatus>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, HttpMethod.Delete, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return new HttpCallResult<bool>(status.Code, status.Response, status.Data?.Deleted ?? false, status.Ok, null);
    }
    
    /// <summary>
    ///     Modifies vector store. All fields in the existing vector store are replaced with the fields from
    ///     <see cref="request" />.
    /// </summary>
    /// <param name="vectorStoreId">The ID of the assistant to modify.</param>
    /// <param name="request"><see cref="VectorStore" />.</param>
    /// <param name="cancellationToken">Optional, <see cref="CancellationToken" />.</param>
    /// <returns><see cref="VectorStore" />.</returns>
    public Task<HttpCallResult<VectorStore>> ModifyVectorStoreAsync(string vectorStoreId, ModifyVectorStoreRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<VectorStore>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}"), request, cancellationToken);
        
    }
    
    /// <summary>
    ///     Retrieves a list of vector stores. Available only for OpenAI
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<ListResponse<VectorStoreFile>>> ListVectorStoreFilesAsync(string vectorStoreId, ListQuery? query = null,
        CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<ListResponse<VectorStoreFile>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi), $"/{vectorStoreId}/files"), cancellationToken);
    }
}