using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Interactions;

namespace LlmTornado.ManagedAgents;

/// <summary>
/// Managed Agents API. Google: saved Antigravity configurations. Anthropic: use <see cref="AnthropicManagedAgentsEndpoint"/> and <see cref="AnthropicManagedAgentSessionsEndpoint"/>.
/// </summary>
public class ManagedAgentsEndpoint : EndpointBase
{
    internal ManagedAgentsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.ManagedAgents;

    /// <summary>
    /// Creates a managed agent. Google provider only.
    /// </summary>
    public Task<HttpCallResult<ManagedAgent>> Create(ManagedAgentCreateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.Google);
        string url = provider.ApiUrl(CapabilityEndpoints.ManagedAgents, null);
        return HttpPost<ManagedAgent>(provider, CapabilityEndpoints.ManagedAgents, url, request.Serialize(), ct: cancellationToken, headers: request.GetApiRevisionHeaders());
    }

    /// <summary>
    /// Gets a managed agent by ID.
    /// </summary>
    public Task<HttpCallResult<ManagedAgent>> Get(string agentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.Google);
        string url = GetUrl(provider, $"/{agentId}");
        return HttpGet<ManagedAgent>(provider, CapabilityEndpoints.ManagedAgents, url, ct: cancellationToken);
    }

    /// <summary>
    /// Lists managed agents.
    /// </summary>
    public Task<HttpCallResult<ManagedAgentsListResponse>> List(int? pageSize = null, string? pageToken = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.Google);
        Dictionary<string, object> query = new();

        if (pageSize is not null)
        {
            query["pageSize"] = pageSize.Value;
        }

        if (pageToken is not null)
        {
            query["pageToken"] = pageToken;
        }

        string url = provider.ApiUrl(CapabilityEndpoints.ManagedAgents, null);
        return HttpGet<ManagedAgentsListResponse>(provider, CapabilityEndpoints.ManagedAgents, url, query, ct: cancellationToken);
    }

    /// <summary>
    /// Deletes a managed agent configuration by ID.
    /// </summary>
    public Task<HttpCallResult<bool>> Delete(string agentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.GetProvider(LLmProviders.Google);
        string url = GetUrl(provider, $"/{agentId}");
        return HttpDeleteRaw<bool>(provider, CapabilityEndpoints.ManagedAgents, url, ct: cancellationToken);
    }
}
