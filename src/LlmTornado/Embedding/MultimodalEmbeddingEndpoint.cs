using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Embedding;

/// <summary>
/// The Voyage multimodal embedding endpoint returns vector representations for a given list of multimodal inputs consisting of text, images, or an interleaving of both modalities.
/// </summary>
public class MultimodalEmbeddingEndpoint : EndpointBase
{
    /// <summary>
    /// Constructor of the api endpoint.
    /// </summary>
    internal MultimodalEmbeddingEndpoint(TornadoApi api) : base(api)
    {
        
    }
    
    /// <summary>
    /// The name of the endpoint, which is the final path segment in the API URL.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.MultimodalEmbeddings;
    
    /// <summary>
    /// Ask the API to create embeddings for the given input.
    /// </summary>
    public async Task<MultimodalEmbeddingResult?> CreateMultimodalEmbedding(MultimodalEmbeddingRequest request, CancellationToken ct = default)
    {
        HttpCallResult<MultimodalEmbeddingResult> result = await CreateMultimodalEmbeddingSafe(request, ct).ConfigureAwait(false);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        return result.Data;
    }
    
    /// <summary>
    /// Ask the API to create embeddings for the given input. This method doesn't throw exceptions (even if the network layer fails).
    /// </summary>
    public async Task<HttpCallResult<MultimodalEmbeddingResult>> CreateMultimodalEmbeddingSafe(MultimodalEmbeddingRequest request, CancellationToken ct = default)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model);
        TornadoRequestContent requestBody = request.Serialize(provider);
        return await HttpPost<MultimodalEmbeddingResult>(provider, Endpoint, requestBody.Url, requestBody.Body, request.Model, request, ct).ConfigureAwait(false);
    }
}