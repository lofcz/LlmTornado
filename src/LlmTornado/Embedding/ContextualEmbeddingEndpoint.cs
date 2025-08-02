using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Embedding.Models;

namespace LlmTornado.Embedding;

/// <summary>
/// The Voyage contextualized chunk embedding endpoint accepts document chunks—in addition to queries and full documents—and returns a response containing contextualized chunk vector embeddings.
/// </summary>
public class ContextualEmbeddingEndpoint : EndpointBase
{
    /// <summary>
    /// Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
    /// <see cref="TornadoApi" /> as <see cref="TornadoApi.ContextualEmbeddings" />.
    /// </summary>
    /// <param name="api"></param>
    internal ContextualEmbeddingEndpoint(TornadoApi api) : base(api)
    {
    }
    
    /// <summary>
    /// The name of the endpoint, which is the final path segment in the API URL.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.ContextualEmbeddings;
    
    /// <summary>
    /// Ask the API to create embeddings for the given input.
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>Asynchronously returns the embeddings result.</returns>
    public async Task<ContextualEmbeddingResult?> CreateContextualEmbedding(ContextualEmbeddingRequest request)
    {
        HttpCallResult<ContextualEmbeddingResult> result = await CreateContextualEmbeddingSafe(request).ConfigureAwait(false);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        return result.Data;
    }

    /// <summary>
    /// Ask the API to create embeddings for the given input. This method doesn't throw exceptions (even if the network layer fails).
    /// </summary>
    /// <param name="request">The request to send to the API.</param>
    /// <returns>Asynchronously returns the embeddings result.</returns>
    public async Task<HttpCallResult<ContextualEmbeddingResult>> CreateContextualEmbeddingSafe(ContextualEmbeddingRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model);
        TornadoRequestContent requestBody = request.Serialize(provider);
        return await HttpPost<ContextualEmbeddingResult>(provider, Endpoint, requestBody.Url, requestBody.Body, request.Model, request, CancellationToken.None).ConfigureAwait(false);
    }
}