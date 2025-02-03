using System;
using System.Globalization;
using System.Net.Http;
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
    /// Creates caching endpoint object.
    /// </summary>
    /// <param name="api"></param>
    public CachingEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    /// Caching endpoint.
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Caching;
    
    /// <summary>
    ///	Creates CachedContent resource.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentInformation>> Create(CreateCachedContentRequest request, CancellationToken? cancellationToken = null)
    {
        return HttpPostRaw<CachedContentInformation>(Api.GetProvider(LLmProviders.Google), Endpoint, postData: request.Serialize(LLmProviders.Google), ct: cancellationToken);
    }
    
    /// <summary>
    /// Lists CachedContents.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentList>> List(ListQuery? query = null, CancellationToken? cancellationToken = null)
    {
        return HttpGetRaw<CachedContentList>(Api.GetProvider(LLmProviders.Google), Endpoint, queryParams: query?.ToQueryParams(LLmProviders.Google), ct: cancellationToken);
    }
    
    /// <summary>
    /// Reads CachedContent resource.
    /// </summary>
    /// <param name="name">Name of the resource, should be cachedContents/{id}</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentInformation>> Get(string name, CancellationToken? cancellationToken = null)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.Google);
        return HttpGetRaw<CachedContentInformation>(resolvedProvider, Endpoint, GetUrl(resolvedProvider, CapabilityEndpoints.BaseUrl, name), ct: cancellationToken);
    }
    
    /// <summary>
    /// Updates CachedContent resource (only expiration is updatable).
    /// </summary>
    /// <param name="name">Name of the resource, should be cachedContents/{id}</param>
    /// <param name="newTimeToLive">How long should the cache live</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentInformation>> Patch(string name, TimeSpan newTimeToLive, CancellationToken? cancellationToken = null)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.Google);
        return HttpRequestRaw<CachedContentInformation>(resolvedProvider, Endpoint, GetUrl(resolvedProvider, CapabilityEndpoints.BaseUrl, name), postData: new
        {
            ttl = $"{newTimeToLive.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s"
        }, verb: HttpMethod.Patch, ct: cancellationToken);
    }
    
    /// <summary>
    /// Updates CachedContent resource (only expiration is updatable).
    /// </summary>
    /// <param name="name">Name of the resource, should be cachedContents/{id}</param>
    /// <param name="newSecondsToLive">How long should the cache live</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<HttpCallResult<CachedContentInformation>> Patch(string name, int newSecondsToLive, CancellationToken? cancellationToken = null)
    {
        return Patch(name, TimeSpan.FromSeconds(newSecondsToLive), cancellationToken);
    }
    
    /// <summary>
    /// Deletes CachedContent resource.
    /// </summary>
    /// <param name="name">Name of the resource, should be cachedContents/{id}</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<HttpCallResult<bool>> Delete(string name, CancellationToken? cancellationToken = null)
    {
        IEndpointProvider resolvedProvider = Api.ResolveProvider(LLmProviders.Google);
        HttpCallResult<bool> result = await HttpRequestRaw<bool>(resolvedProvider, Endpoint, GetUrl(resolvedProvider, CapabilityEndpoints.BaseUrl, name), verb: HttpMethod.Delete, ct: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);

        // google returns empty response if ok
        if (result.Ok)
        {
            result.Data = true;
        }

        return result;
    }
}