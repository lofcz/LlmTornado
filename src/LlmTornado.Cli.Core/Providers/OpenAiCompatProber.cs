using System.Net.Http.Headers;
using System.Text.Json;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Providers;

/// <summary>
/// Probes an OpenAI-compatible <c>GET {base}/models</c> endpoint and maps results to
/// <see cref="ChatModel"/> instances under <see cref="LLmProviders.Custom"/>.
/// </summary>
public static class OpenAiCompatProber
{
    /// <summary>
    /// Default context window assumed when neither the endpoint config nor the model card
    /// reports one. Prefer the compression cap over this when available at the call site.
    /// </summary>
    public const int DefaultUnknownContextTokens = 8192;

    public static List<ChatModel> ProbeModels(OpenAiCompatEndpoint endpoint, out string? warning)
    {
        warning = null;
        List<ChatModel> models = [];

        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
            using HttpRequestMessage request = new(HttpMethod.Get, $"{endpoint.BaseUrl.TrimEnd('/')}/models");
            if (!string.IsNullOrWhiteSpace(endpoint.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.ApiKey);

            using HttpResponseMessage response = client.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                warning = $"Endpoint '{endpoint.Name}' returned HTTP {(int)response.StatusCode} from {endpoint.BaseUrl}/models.";
                return models;
            }

            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out JsonElement data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                warning = $"Endpoint '{endpoint.Name}' returned an unexpected /models payload.";
                return models;
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out JsonElement idElement))
                    continue;

                string? id = idElement.GetString();
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    continue;

                int? context = endpoint.ContextTokens;
                if (item.TryGetProperty("context_length", out JsonElement ctxEl) &&
                    ctxEl.TryGetInt32(out int reported) && reported > 0)
                {
                    context = reported;
                }
                else if (item.TryGetProperty("max_model_len", out JsonElement maxEl) &&
                         maxEl.TryGetInt32(out int maxLen) && maxLen > 0)
                {
                    context = maxLen;
                }

                models.Add(context is > 0
                    ? new ChatModel(id, LLmProviders.Custom, context.Value)
                    : new ChatModel(id, LLmProviders.Custom));
            }

            if (models.Count == 0)
                warning = $"Endpoint '{endpoint.Name}' is reachable but listed no models.";
        }
        catch (Exception ex)
        {
            warning = $"Endpoint '{endpoint.Name}' unreachable at {endpoint.BaseUrl}: {ex.Message}";
        }

        return models;
    }

    /// <summary>
    /// Build a dedicated <see cref="TornadoApi"/> for a single OpenAI-compatible base URL.
    /// A <see cref="TornadoApi"/> can only hold one Custom BaseUrl, so each endpoint needs its own.
    /// </summary>
    public static TornadoApi CreateApi(OpenAiCompatEndpoint endpoint)
    {
        string key = endpoint.ApiKey ?? string.Empty;
        return new TornadoApi(new Uri(endpoint.BaseUrl.TrimEnd('/') + "/"), key, LLmProviders.Custom);
    }

    /// <summary>
    /// Resolve a usable context window for a Custom/local model when the model card is silent.
    /// Order: model.ContextTokens → endpoint default → compression cap → 8192.
    /// </summary>
    public static int ResolveContextTokens(int? modelContextTokens, int? endpointDefault, int? compressionCap)
    {
        if (modelContextTokens is > 0)
            return modelContextTokens.Value;
        if (endpointDefault is > 0)
            return endpointDefault.Value;
        if (compressionCap is > 0)
            return compressionCap.Value;
        return DefaultUnknownContextTokens;
    }
}
