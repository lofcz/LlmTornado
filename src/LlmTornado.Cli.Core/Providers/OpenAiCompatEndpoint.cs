using System.Text.Json.Serialization;

namespace LlmTornado.Cli.Core.Providers;

/// <summary>
/// A user-configured OpenAI-compatible HTTP endpoint (LM Studio, llama.cpp, vLLM, etc.).
/// Also used as the settings DTO for <c>openai_compat_endpoints</c>.
/// </summary>
public sealed class OpenAiCompatEndpoint
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// OpenAI-compatible base URL, typically including the <c>/v1</c> suffix
    /// (e.g. <c>http://localhost:1234/v1</c>).
    /// </summary>
    [JsonPropertyName("base_url")]
    public required string BaseUrl { get; set; }

    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional default context window applied to every model discovered on this endpoint
    /// when the server does not report one.
    /// </summary>
    [JsonPropertyName("context_tokens")]
    public int? ContextTokens { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Parse <c>TORNADO_OPENAI_COMPAT</c>: <c>name=url[|key][|ctx],...</c>.
    /// Invalid entries are skipped.
    /// </summary>
    public static List<OpenAiCompatEndpoint> ParseEnv(string? envValue)
    {
        List<OpenAiCompatEndpoint> result = [];
        if (string.IsNullOrWhiteSpace(envValue))
            return result;

        foreach (string entry in envValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = entry.IndexOf('=');
            if (eq <= 0 || eq >= entry.Length - 1)
                continue;

            string name = entry[..eq].Trim();
            string rest = entry[(eq + 1)..].Trim();
            if (name.Length == 0 || rest.Length == 0)
                continue;

            string[] parts = rest.Split('|', StringSplitOptions.TrimEntries);
            string baseUrl = parts[0];
            if (baseUrl.Length == 0)
                continue;

            string? apiKey = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null;
            int? contextTokens = null;
            if (parts.Length > 2 && int.TryParse(parts[2], out int ctx) && ctx > 0)
                contextTokens = ctx;

            result.Add(new OpenAiCompatEndpoint
            {
                Name = name,
                BaseUrl = NormalizeBaseUrl(baseUrl),
                ApiKey = apiKey,
                ContextTokens = contextTokens,
                Enabled = true,
            });
        }

        return result;
    }

    /// <summary>
    /// Merge settings + env endpoints. Settings win by name (case-insensitive).
    /// Disabled settings entries suppress the env entry of the same name.
    /// </summary>
    public static List<OpenAiCompatEndpoint> Merge(
        IEnumerable<OpenAiCompatEndpoint>? fromSettings,
        IEnumerable<OpenAiCompatEndpoint>? fromEnv)
    {
        Dictionary<string, OpenAiCompatEndpoint> map = new(StringComparer.OrdinalIgnoreCase);

        if (fromEnv is not null)
        {
            foreach (OpenAiCompatEndpoint ep in fromEnv)
            {
                if (string.IsNullOrWhiteSpace(ep.Name) || string.IsNullOrWhiteSpace(ep.BaseUrl))
                    continue;
                map[ep.Name.Trim()] = CloneNormalized(ep);
            }
        }

        if (fromSettings is not null)
        {
            foreach (OpenAiCompatEndpoint ep in fromSettings)
            {
                if (string.IsNullOrWhiteSpace(ep.Name) || string.IsNullOrWhiteSpace(ep.BaseUrl))
                    continue;
                map[ep.Name.Trim()] = CloneNormalized(ep);
            }
        }

        return map.Values
            .Where(e => e.Enabled)
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeBaseUrl(string baseUrl)
    {
        baseUrl = baseUrl.Trim().TrimEnd('/');
        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "http://" + baseUrl;
        }

        return baseUrl.TrimEnd('/');
    }

    private static OpenAiCompatEndpoint CloneNormalized(OpenAiCompatEndpoint ep) => new()
    {
        Name = ep.Name.Trim(),
        BaseUrl = NormalizeBaseUrl(ep.BaseUrl),
        ApiKey = string.IsNullOrWhiteSpace(ep.ApiKey) ? null : ep.ApiKey.Trim(),
        ContextTokens = ep.ContextTokens is > 0 ? ep.ContextTokens : null,
        Enabled = ep.Enabled,
    };
}
