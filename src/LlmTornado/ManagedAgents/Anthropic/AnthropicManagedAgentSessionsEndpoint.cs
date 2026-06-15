using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Threads;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Claude Managed Agent sessions — create, run, send events, and stream SSE responses.
/// </summary>
public class AnthropicManagedAgentSessionsEndpoint : EndpointBase
{
    internal AnthropicManagedAgentSessionsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.AnthropicManagedAgentSessions;

    /// <summary>
    /// Creates a session (<c>POST /v1/sessions</c>).
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentSession>> Create(AnthropicManagedAgentSessionCreateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgentSession>(provider, Endpoint, postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentSession>> Get(string sessionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<AnthropicManagedAgentSession>(provider, Endpoint, GetResourcePath(sessionId), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Lists sessions.
    /// </summary>
    public Task<HttpCallResult<ListResponse<AnthropicManagedAgentSession>>> List(ListQuery? query = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<ListResponse<AnthropicManagedAgentSession>>(
            provider,
            Endpoint,
            queryParams: ListQuery.ToQueryParams(LLmProviders.Anthropic, query),
            ct: cancellationToken,
            headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Updates session metadata, title, or session-local agent tool overrides.
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentSession>> Update(string sessionId, AnthropicManagedAgentSessionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgentSession>(provider, Endpoint, GetResourcePath(sessionId), postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Deletes a session and its server-side state.
    /// </summary>
    public Task<HttpCallResult<bool>> Delete(string sessionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpDeleteRaw<bool>(provider, Endpoint, GetResourcePath(sessionId), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Archives a session (read-only, not reversible).
    /// </summary>
    public Task<HttpCallResult<AnthropicManagedAgentSession>> Archive(string sessionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<AnthropicManagedAgentSession>(provider, Endpoint, $"{GetResourcePath(sessionId)}/archive", ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Sends events to a session (user messages, tool results, interrupts).
    /// </summary>
    public Task<HttpCallResult<object>> SendEvents(string sessionId, AnthropicManagedAgentSendEventsRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpPost<object>(provider, Endpoint, $"{GetResourcePath(sessionId)}/events", postData: request.Serialize(), ct: cancellationToken, headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Lists session events (paginated polling).
    /// </summary>
    public Task<HttpCallResult<ListResponse<AnthropicManagedAgentEvent>>> ListEvents(string sessionId, ListQuery? query = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);
        return HttpGet<ListResponse<AnthropicManagedAgentEvent>>(
            provider,
            Endpoint,
            $"{GetResourcePath(sessionId)}/events",
            queryParams: ListQuery.ToQueryParams(LLmProviders.Anthropic, query),
            ct: cancellationToken,
            headers: VendorAnthropicManagedAgentsConstants.ApiHeaders);
    }

    /// <summary>
    /// Streams session events via SSE (<c>GET /v1/sessions/{id}/events/stream</c>).
    /// Open the stream before sending user events to avoid missing early lifecycle events.
    /// </summary>
    public async IAsyncEnumerable<AnthropicManagedAgentStreamEvent> StreamEvents(
        string sessionId,
        AnthropicManagedAgentStreamEventHandler? handler = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Anthropic);

        TornadoStreamRequest streamRequest = await HttpStreamingRequestData(
            provider,
            Endpoint,
            url: $"{GetResourcePath(sessionId)}/events/stream",
            verb: HttpVerbs.Get,
            headers: VendorAnthropicManagedAgentsConstants.ApiHeaders,
            token: cancellationToken).ConfigureAwait(false);

        if (streamRequest.Exception is not null)
        {
            throw streamRequest.Exception;
        }

        await foreach (ServerSentEvent sse in provider.InboundStream(streamRequest.StreamReader).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (handler?.OnSse is not null)
            {
                await handler.OnSse(sse).ConfigureAwait(false);
            }

            AnthropicManagedAgentStreamEvent evt = AnthropicManagedAgentStreamEvent.FromServerSentEvent(sse);

            if (handler?.OnTextDelta is not null && evt.Event?.Content is not null)
            {
                foreach (Newtonsoft.Json.Linq.JToken block in evt.Event.Content)
                {
                    string? text = block["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        await handler.OnTextDelta(text).ConfigureAwait(false);
                    }
                }
            }

            if (handler?.OnSessionIdle is not null && evt.Event?.Type is "session.status_idle")
            {
                await handler.OnSessionIdle(evt.Event).ConfigureAwait(false);
            }

            if (handler?.OnEvent is not null)
            {
                await handler.OnEvent(evt).ConfigureAwait(false);
            }

            yield return evt;
        }
    }

    private static string GetResourcePath(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new System.ArgumentException("Session id is required.", nameof(sessionId));
        }

        return sessionId.StartsWith('/') ? sessionId : $"/{sessionId}";
    }
}
