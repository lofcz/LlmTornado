using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Claude Managed Agent environments — sandbox templates required before starting sessions.
/// </summary>
public class AnthropicManagedAgentEnvironmentsEndpoint : EndpointBase
{
    internal AnthropicManagedAgentEnvironmentsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.AnthropicManagedAgentEnvironments;

    /// <summary>
    /// Creates an environment (<c>POST /v1/environments</c>).
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentEnvironment>> Create(AnthropicManagedAgentEnvironmentCreateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgentEnvironment>(provider, Endpoint, postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Gets an environment by ID.
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentEnvironment>> Get(string environmentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<AnthropicManagedAgentEnvironment>(provider, Endpoint, GetResourcePath(environmentId), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Lists environments.
    /// </summary>
    public Task<HttpCallResult<ListResponse<AnthropicManagedAgentEnvironment>>> List(ListQuery? query = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<ListResponse<AnthropicManagedAgentEnvironment>>(
            provider,
            Endpoint,
            queryParams: ListQuery.ToQueryParams(LLmProviders.Anthropic, query),
            ct: cancellationToken,
            headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    private static string GetResourcePath(string environmentId)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            throw new System.ArgumentException("Environment id is required.", nameof(environmentId));
        }

        return environmentId.StartsWith('/') ? environmentId : $"/{environmentId}";
    }
}
