using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Moderation;

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
    public Task<HttpCallResult<VectorStore>> CreateVectorStoreAsync(VectorStoreRequest request,
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
        return HttpPostRaw<ListResponse<VectorStore>>(Api.GetProvider(LLmProviders.OpenAi), Endpoint,
            GetUrl(Api.GetProvider(LLmProviders.OpenAi)), cancellationToken);
    }
}