using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Rerank;

/// <summary>
/// Voyage reranker endpoint receives as input a query, a list of documents, and other arguments such as the model name, and returns a response containing the reranking results.
/// </summary>
public class RerankEndpoint : EndpointBase
{
    /// <summary>
    /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
    /// <see cref="TornadoApi" /> as <see cref="TornadoApi.Rerank" />.
    /// </summary>
    /// <param name="api"></param>
    internal RerankEndpoint(TornadoApi api) : base(api)
    {
    }
    
    /// <summary>
    /// The name of the endpoint, which is the final path segment in the API URL.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Rerank;
    
    /// <summary>
    /// Ask the API to rerank the given documents.
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>Asynchronously returns the rerank result.</returns>
    public async Task<RerankResult?> CreateRerank(RerankRequest request)
    {
        HttpCallResult<RerankResult> result = await CreateRerankSafe(request).ConfigureAwait(false);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        return result.Data;
    }

    /// <summary>
    /// Ask the API to rerank the given documents. This method doesn't throw exceptions (even if the network layer fails).
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>Asynchronously returns the rerank result.</returns>
    public async Task<HttpCallResult<RerankResult>> CreateRerankSafe(RerankRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model);
        TornadoRequestContent requestBody = request.Serialize(provider);
        return await HttpPost<RerankResult>(provider, Endpoint, requestBody.Url, requestBody.Body, request.Model, request, CancellationToken.None).ConfigureAwait(false);
    }
}