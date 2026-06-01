using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Webhooks;

/// <summary>
/// Endpoint for managing Gemini project-level webhooks and handling inbound event payloads.
/// Currently supported for <see cref="LLmProviders.Google"/> only.
/// </summary>
public class WebhooksEndpoint : EndpointBase
{
    internal WebhooksEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <inheritdoc />
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Webhooks;

    /// <summary>
    /// Creates a project-level webhook endpoint.
    /// The response includes <see cref="GeminiWebhook.NewSigningSecret"/> exactly once; store it securely.
    /// </summary>
    public Task<HttpCallResult<GeminiWebhook>> Create(CreateGeminiWebhookRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpPost<GeminiWebhook>(provider, Endpoint, postData: request.Serialize(), ct: cancellationToken);
    }

    /// <summary>
    /// Retrieves a webhook by id.
    /// </summary>
    /// <param name="webhookId">Webhook id or resource name (webhooks/{id}).</param>
    public Task<HttpCallResult<GeminiWebhook>> Get(string webhookId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpGet<GeminiWebhook>(provider, Endpoint, GetWebhookPath(webhookId), ct: cancellationToken);
    }

    /// <summary>
    /// Lists all webhooks configured for the project.
    /// </summary>
    public Task<HttpCallResult<GeminiWebhookList>> List(ListQuery? query = null, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpGet<GeminiWebhookList>(provider, Endpoint, queryParams: query?.ToQueryParams(LLmProviders.Google), ct: cancellationToken);
    }

    /// <summary>
    /// Updates webhook properties (display name, URI, subscribed events).
    /// </summary>
    public Task<HttpCallResult<GeminiWebhook>> Update(string webhookId, UpdateGeminiWebhookRequest request, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpRequestRaw<GeminiWebhook>(provider, Endpoint, GetWebhookPath(webhookId), postData: request.Serialize(), verb: HttpVerbs.Patch, ct: cancellationToken);
    }

    /// <summary>
    /// Deletes a webhook endpoint.
    /// </summary>
    public Task<HttpCallResult<bool>> Delete(string webhookId, CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        return HttpDelete<bool>(provider, Endpoint, GetWebhookPath(webhookId), ct: cancellationToken);
    }

    /// <summary>
    /// Rotates the signing secret for a webhook.
    /// The new secret is returned exactly once in the response.
    /// </summary>
    public Task<HttpCallResult<RotateGeminiWebhookSigningSecretResponse>> RotateSigningSecret(
        string webhookId,
        RotateGeminiWebhookSigningSecretRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        IEndpointProvider provider = Api.ResolveProvider(LLmProviders.Google);
        string path = $"{GetWebhookPath(webhookId)}/rotate_secret";
        return HttpPost<RotateGeminiWebhookSigningSecretResponse>(provider, Endpoint, path, postData: (request ?? new RotateGeminiWebhookSigningSecretRequest()).Serialize(), ct: cancellationToken);
    }

    /// <summary>
    /// Verifies a static webhook delivery and parses the event envelope.
    /// </summary>
    public GeminiWebhookEvent VerifyStaticDelivery(
        string payload,
        System.Collections.Generic.IReadOnlyDictionary<string, string> headers,
        string signingSecret,
        System.TimeSpan? timestampTolerance = null)
    {
        return GeminiWebhookSignatureVerifier.VerifyStaticDelivery(payload, headers, signingSecret, timestampTolerance);
    }

    private static string GetWebhookPath(string webhookId)
    {
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            throw new System.ArgumentException("Webhook id is required.", nameof(webhookId));
        }

        return webhookId.StartsWith("webhooks/", System.StringComparison.Ordinal) ? webhookId : $"webhooks/{webhookId}";
    }
}
