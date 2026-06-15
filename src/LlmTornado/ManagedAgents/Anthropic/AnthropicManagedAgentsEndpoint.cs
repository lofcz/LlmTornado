using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Claude Managed Agents API — create, list, get, update, and archive saved agent configurations.
/// Requires <see cref="VendorAnthropicManagedAgentsConstants.BetaHeader"/> on all requests.
/// </summary>
public class AnthropicManagedAgentsEndpoint : EndpointBase
{
    internal AnthropicManagedAgentsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.AnthropicManagedAgents;

    /// <summary>
    /// Creates a managed agent (<c>POST /v1/agents</c>).
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgent>> Create(AnthropicManagedAgentCreateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgent>(provider, Endpoint, postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgent>> Get(string agentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<AnthropicManagedAgent>(provider, Endpoint, GetResourcePath(agentId), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Lists agents with optional pagination.
    /// </summary>
    public Task<HttpCallResult<ListResponse<AnthropicManagedAgent>>> List(ListQuery? query = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<ListResponse<AnthropicManagedAgent>>(
            provider,
            Endpoint,
            queryParams: ListQuery.ToQueryParams(LLmProviders.Anthropic, query),
            ct: cancellationToken,
            headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Updates an agent (creates a new immutable version).
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgent>> Update(string agentId, AnthropicManagedAgentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgent>(provider, Endpoint, GetResourcePath(agentId), postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Archives an agent permanently.
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgent>> Archive(string agentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgent>(provider, Endpoint, $"{GetResourcePath(agentId)}/archive", ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    private static string GetResourcePath(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new System.ArgumentException("Agent id is required.", nameof(agentId));
        }

        return agentId.StartsWith('/') ? agentId : $"/{agentId}";
    }
}
