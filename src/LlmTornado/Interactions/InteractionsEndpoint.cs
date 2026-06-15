using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Threads;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

/// <summary>
/// Gemini Interactions API endpoint (models and managed agents).
/// Uses the May 2026 steps schema by default via <c>Api-Revision: 2026-05-20</c>.
/// </summary>
public class InteractionsEndpoint : EndpointBase
{
    internal InteractionsEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Interactions;

    /// <summary>
    /// Runs the Antigravity managed agent in a fresh remote sandbox.
    /// </summary>
    public Task<HttpCallResult<Interaction>> CreateAntigravity(string input, CancellationToken cancellationToken = default)
    {
        return Create(InteractionCreateRequest.ForAntigravity(input), cancellationToken);
    }

    /// <summary>
    /// Continues a prior Antigravity interaction in the same sandbox and conversation context.
    /// </summary>
    public Task<HttpCallResult<Interaction>> ContinueAntigravity(string input, Interaction previous, CancellationToken cancellationToken = default)
    {
        InteractionCreateRequest request = InteractionCreateRequest.ForAntigravity(input);
        request.PreviousInteractionId = previous.Id;

        string? environmentId = previous.EnvironmentId;
        if (!string.IsNullOrEmpty(environmentId))
        {
            request.Environment = InteractionEnvironmentReference.FromId(environmentId);
        }

        return Create(request, cancellationToken);
    }

    /// <summary>
    /// Creates an interaction (unary).
    /// </summary>
    public Task<HttpCallResult<Interaction>> Create(InteractionCreateRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpPost<Interaction>(provider, Endpoint, postData: request.Serialize(), headers: request.GetApiRevisionHeaders(), requestObj: request, ct: cancellationToken);
    }

    /// <summary>
    /// Retrieves a stored interaction by ID.
    /// </summary>
    public Task<HttpCallResult<Interaction>> Get(string interactionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpGet<Interaction>(provider, Endpoint, GetInteractionPath(interactionId), ct: cancellationToken);
    }

    /// <summary>
    /// Deletes a stored interaction.
    /// </summary>
    public Task<HttpCallResult<bool>> Delete(string interactionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpDelete<bool>(provider, Endpoint, GetInteractionPath(interactionId), ct: cancellationToken);
    }

    /// <summary>
    /// Cancels a background interaction.
    /// </summary>
    public Task<HttpCallResult<Interaction>> Cancel(string interactionId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpPost<Interaction>(provider, Endpoint, $"{GetInteractionPath(interactionId)}:cancel", ct: cancellationToken);
    }

    /// <summary>
    /// Creates a streaming interaction and yields parsed SSE events.
    /// </summary>
    public async IAsyncEnumerable<InteractionStreamEvent> CreateStream(
        InteractionCreateRequest request,
        InteractionStreamEventHandler? handler = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);

        TornadoStreamRequest streamRequest = await HttpStreamingRequestData(
            provider,
            Endpoint,
            postData: request.Serialize(),
            requestObj: request,
            headers: request.GetApiRevisionHeaders(),
            token: cancellationToken).ConfigureAwait(false);

        if (streamRequest.Exception is not null)
        {
            throw streamRequest.Exception;
        }

        if (streamRequest.StreamReader is null)
        {
            yield break;
        }

        StreamReader reader = streamRequest.StreamReader;
        await foreach (ServerSentEvent sse in provider.InboundStream(reader).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (handler?.OnSse is not null)
            {
                await handler.OnSse(sse).ConfigureAwait(false);
            }

            InteractionStreamEvent evt = InteractionStreamEvent.FromServerSentEvent(sse);
            await DispatchStreamHandlers(handler, evt).ConfigureAwait(false);

            if (handler?.OnEvent is not null)
            {
                await handler.OnEvent(evt).ConfigureAwait(false);
            }

            yield return evt;
        }
    }

    /// <summary>
    /// Resumes streaming for an in-progress interaction (e.g. after SSE disconnect).
    /// </summary>
    public IAsyncEnumerable<InteractionStreamEvent> GetStream(
        string interactionId,
        string? lastEventId = null,
        InteractionStreamEventHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> query = new() { ["stream"] = true };
        if (!string.IsNullOrEmpty(lastEventId))
        {
            query["last_event_id"] = lastEventId!;
        }

        return StreamGetInternal(interactionId, query, handler, cancellationToken);
    }

    /// <summary>
    /// Polls a background interaction until it reaches a terminal state.
    /// </summary>
    public async Task<HttpCallResult<Interaction>> WaitForCompletion(
        string interactionId,
        int pollingIntervalMs = 5000,
        int maxWaitMs = 3_600_000,
        CancellationToken cancellationToken = default)
    {
        DateTime start = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpCallResult<Interaction> result = await Get(interactionId, cancellationToken).ConfigureAwait(false);

            if (!result.Ok || result.Data is null)
            {
                return result;
            }

            InteractionStatus status = result.Data.StatusEnum;
            if (status is InteractionStatus.Completed or InteractionStatus.Failed or InteractionStatus.Cancelled or InteractionStatus.Incomplete or InteractionStatus.BudgetExceeded)
            {
                return result;
            }

            if ((DateTime.UtcNow - start).TotalMilliseconds > maxWaitMs)
            {
                return result;
            }

            await Task.Delay(pollingIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return await Get(interactionId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a Deep Research interaction and waits for completion.
    /// </summary>
    public async Task<HttpCallResult<Interaction>> CreateDeepResearchAndWait(
        InteractionCreateRequest request,
        int pollingIntervalMs = 5000,
        int maxWaitMs = 3_600_000,
        CancellationToken cancellationToken = default)
    {
        request.Background ??= true;
        request.Store ??= true;

        HttpCallResult<Interaction> createResult = await Create(request, cancellationToken).ConfigureAwait(false);
        if (!createResult.Ok || createResult.Data?.Id is null)
        {
            return createResult;
        }

        return await WaitForCompletion(createResult.Data.Id, pollingIntervalMs, maxWaitMs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams a Deep Research interaction with automatic SSE reconnection while status is <c>in_progress</c>.
    /// </summary>
    public async Task StreamDeepResearchWithReconnect(
        InteractionCreateRequest request,
        InteractionStreamEventHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        request.Background = true;
        request.Store = true;

        string? interactionId = null;
        string? lastEventId = null;
        bool isComplete = false;

        await foreach (InteractionStreamEvent evt in CreateStream(request, handler, cancellationToken).ConfigureAwait(false))
        {
            interactionId ??= evt.Interaction?.Id;
            lastEventId = ExtractEventId(evt) ?? lastEventId;
            if (evt.NormalizedEventType is InteractionStreamEventTypes.InteractionCompleted or InteractionStreamEventTypes.Error)
            {
                isComplete = true;
            }
        }

        while (!isComplete && !string.IsNullOrEmpty(interactionId) && !cancellationToken.IsCancellationRequested)
        {
            HttpCallResult<Interaction> status = await Get(interactionId, cancellationToken).ConfigureAwait(false);
            if (!status.Ok || status.Data?.StatusEnum != InteractionStatus.InProgress)
            {
                break;
            }

            await foreach (InteractionStreamEvent evt in GetStream(interactionId, lastEventId, handler, cancellationToken).ConfigureAwait(false))
            {
                lastEventId = ExtractEventId(evt) ?? lastEventId;
                if (evt.NormalizedEventType is InteractionStreamEventTypes.InteractionCompleted or InteractionStreamEventTypes.Error)
                {
                    isComplete = true;
                    break;
                }
            }
        }
    }

    private async IAsyncEnumerable<InteractionStreamEvent> StreamGetInternal(
        string interactionId,
        Dictionary<string, object> query,
        InteractionStreamEventHandler? handler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        string url = GetUrl(provider, GetInteractionPath(interactionId));

        TornadoStreamRequest streamRequest = await HttpStreamingRequestData(
            provider,
            Endpoint,
            url,
            query,
            HttpVerbs.Get,
            requestObj: null,
            token: cancellationToken,
            headers: new Dictionary<string, object?> { ["Api-Revision"] = InteractionSchemaRevision.May2026.ToHeaderValue()! }).ConfigureAwait(false);

        if (streamRequest.Exception is not null)
        {
            throw streamRequest.Exception;
        }

        if (streamRequest.StreamReader is null)
        {
            yield break;
        }

        StreamReader reader = streamRequest.StreamReader;
        await foreach (ServerSentEvent sse in provider.InboundStream(reader).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (handler?.OnSse is not null)
            {
                await handler.OnSse(sse).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(sse.Data) || sse.Data == "[DONE]")
            {
                yield break;
            }

            InteractionStreamEvent evt = InteractionStreamEvent.FromServerSentEvent(sse);
            await DispatchStreamHandlers(handler, evt).ConfigureAwait(false);

            if (handler?.OnEvent is not null)
            {
                await handler.OnEvent(evt).ConfigureAwait(false);
            }

            yield return evt;
        }
    }

    private static async ValueTask DispatchStreamHandlers(InteractionStreamEventHandler? handler, InteractionStreamEvent evt)
    {
        if (handler is null)
        {
            return;
        }

        string? deltaType = evt.Delta?["type"]?.ToString();
        string? text = evt.Delta?["text"]?.ToString() ?? evt.Delta?["content"]?["text"]?.ToString();

        if (handler.OnTextDelta is not null && deltaType == "text" && text is { Length: > 0 })
        {
            await handler.OnTextDelta(text).ConfigureAwait(false);
        }

        if (handler.OnThoughtDelta is not null && deltaType is "thought" or "thought_summary" && text is { Length: > 0 })
        {
            await handler.OnThoughtDelta(text).ConfigureAwait(false);
        }

        if (handler.OnImageDelta is not null && deltaType == "image" && evt.Delta?["data"]?.ToString() is { Length: > 0 } imageData)
        {
            await handler.OnImageDelta(imageData).ConfigureAwait(false);
        }

        if (handler.OnCompleted is not null && evt.NormalizedEventType is InteractionStreamEventTypes.InteractionCompleted && evt.Interaction is not null)
        {
            await handler.OnCompleted(evt.Interaction).ConfigureAwait(false);
        }

        if (handler.OnError is not null && evt.NormalizedEventType is InteractionStreamEventTypes.Error)
        {
            InteractionError error = new InteractionError
            {
                Message = evt.Delta?["message"]?.ToString() ?? evt.Data
            };
            await handler.OnError(error).ConfigureAwait(false);
        }
    }

    private static string? ExtractEventId(InteractionStreamEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.Data))
        {
            return null;
        }

        try
        {
            return JObject.Parse(evt.Data)["event_id"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string GetInteractionPath(string interactionId)
    {
        if (string.IsNullOrWhiteSpace(interactionId))
        {
            throw new System.ArgumentException("Interaction id is required.", nameof(interactionId));
        }

        return interactionId.StartsWith("interactions/", System.StringComparison.Ordinal) ? interactionId : interactionId;
    }

    /// <summary>
    /// Downloads a tarball snapshot of all files in a managed agent environment.
    /// </summary>
    /// <param name="environmentId">Value from <see cref="Interaction.EnvironmentId"/>.</param>
    public Task<StreamResponse?> DownloadEnvironmentSnapshot(string environmentId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        ProviderAuthentication? auth = Api.GetProvider(LLmProviders.Google).Auth;
        string url = $"{GoogleEndpointProvider.BaseUrl}v1beta/files/environment-{environmentId}:download?alt=media";

        Dictionary<string, string> headers = new();
        if (auth?.ApiKey is not null)
        {
            headers["x-goog-api-key"] = auth.ApiKey.Trim();
        }

        return HttpGetRawStream(provider, url, headers, cancellationToken);
    }
}
