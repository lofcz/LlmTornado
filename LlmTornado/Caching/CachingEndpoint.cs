using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.VectorStores;

namespace LlmTornado.Caching;

/// <summary>
/// This endpoint can be used for message generation caching. Currently used only by <see cref="LLmProviders.Google"/>, other providers have different caching mechanisms. 
/// </summary>
public class CachingEndpoint : EndpointBase
{
    /// <summary>
    ///     Creates caching endpoint object.
    /// </summary>
    /// <param name="api"></param>
    public CachingEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     Caching endpoint.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Caching;
    
    /// <summary>
    ///		Create a vector store. Available only for OpenAI
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentInformation>> Create(CreateCachedContentRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<CachedContentInformation>(Api.GetProvider(LLmProviders.Google), Endpoint, postData: request.Serialize(LLmProviders.Google), ct: cancellationToken);
    }
}