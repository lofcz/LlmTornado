using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Commands;

internal static class OllamaContextInspector
{
    private const string DefaultOllamaHost = "http://localhost:11434";
    private const int NetworkTimeoutSeconds = 2;
    private const int ProcessTimeoutMilliseconds = 2000;

    public static async Task<int?> TryGetContextTokens(string modelName, string host)
    {
        int? runtimeContext = await TryGetRuntimeContextTokens(modelName, host);
        if (runtimeContext is > 0)
            return runtimeContext;

        return await TryGetModelCardContextTokens(modelName, host);
    }

    internal static async Task<int?> TryGetModelCardContextTokens(string modelName, string host)
    {
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(NetworkTimeoutSeconds) };
            using StringContent content = new(
                JsonSerializer.Serialize(new { model = modelName }),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await client.PostAsync($"{host}/api/show", content);
            if (!response.IsSuccessStatusCode)
                return null;

            string body = await response.Content.ReadAsStringAsync();
            return TryExtractContextTokens(body);
        }
        catch
        {
            // Best-effort diagnostics only.
            return null;
        }
    }

    internal static async Task<int?> TryGetRuntimeContextTokens(string modelName, string host)
    {
        int? fromApi = await TryGetRuntimeContextTokensFromApi(host, modelName);
        if (fromApi is > 0)
            return fromApi;

        return TryGetRuntimeContextTokensFromPs(modelName);
    }

    private static async Task<int?> TryGetRuntimeContextTokensFromApi(string host, string modelName)
    {
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(NetworkTimeoutSeconds) };
            string json = await client.GetStringAsync($"{host}/api/ps");
            return TryExtractRuntimeContextTokensFromPsJson(json, modelName);
        }
        catch
        {
            return null;
        }
    }

    internal static int? TryExtractRuntimeContextTokensFromPsJson(string json, string modelName)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("models", out JsonElement models) || models.ValueKind != JsonValueKind.Array)
                return null;

            foreach (JsonElement model in models.EnumerateArray())
            {
                if (!IsMatchingModel(model, modelName))
                    continue;

                int? context = TryReadRuntimeContextFromElement(model);
                if (context is > 0)
                    return context;
            }
        }
        catch
        {
            // Ignore parse failures for best-effort path.
        }

        return null;
    }

    private static int? TryGetRuntimeContextTokensFromPs(string modelName)
    {
        string? jsonOutput = TryRunOllamaPs("ps --json");
        if (!string.IsNullOrWhiteSpace(jsonOutput))
        {
            int? parsedJson = TryExtractRuntimeContextTokensFromPsJson(jsonOutput, modelName);
            if (parsedJson is > 0)
                return parsedJson;
        }

        string? textOutput = TryRunOllamaPs("ps");
        if (string.IsNullOrWhiteSpace(textOutput))
            return null;

        return TryExtractRuntimeContextTokensFromPsText(textOutput, modelName);
    }

    private static string? TryRunOllamaPs(string arguments)
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "ollama",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process process = Process.Start(psi)!;
            if (!process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return null;
            }

            if (process.ExitCode != 0)
                return null;

            return process.StandardOutput.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }

    internal static int? TryExtractRuntimeContextTokensFromPsText(string psOutput, string modelName)
    {
        string[] rawLines = psOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (rawLines.Length < 2)
            return null;

        string header = rawLines[0];
        string[] headerColumns = Regex.Split(header, @"\s{2,}|\t+");
        int contextColumnIndex = Array.FindIndex(
            headerColumns,
            x => x.Equals("CONTEXT", StringComparison.OrdinalIgnoreCase) ||
                 x.Equals("CTX", StringComparison.OrdinalIgnoreCase));

        if (contextColumnIndex < 0)
            return null;

        for (int i = 1; i < rawLines.Length; i++)
        {
            string line = rawLines[i];
            string[] columns = Regex.Split(line, @"\s{2,}|\t+");
            if (columns.Length <= contextColumnIndex)
                continue;

            string rowModelName = columns[0];
            if (!NormalizeModelName(rowModelName).Equals(NormalizeModelName(modelName), StringComparison.Ordinal))
                continue;

            if (TryParseContextTokenValue(columns[contextColumnIndex], out int parsed))
                return parsed;
        }

        return null;
    }

    public static int? TryExtractContextTokens(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("model_info", out JsonElement modelInfo) &&
                TryReadContextFromModelInfo(modelInfo, out int modelInfoContext))
            {
                return modelInfoContext;
            }

            if (doc.RootElement.TryGetProperty("parameters", out JsonElement parameters) &&
                TryReadContextFromParameters(parameters, out int parameterContext))
            {
                return parameterContext;
            }
        }
        catch
        {
            // Parse failures should be treated as unknown context size.
        }

        return null;
    }

    public static string ResolveHost(string? hostValue)
    {
        string host = hostValue ?? DefaultOllamaHost;
        host = host.Trim();

        if (host.Length == 0)
            return DefaultOllamaHost;

        if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            host = "http://" + host;
        }

        host = host.TrimEnd('/');

        if (host.Equals("http://0.0.0.0", StringComparison.OrdinalIgnoreCase))
            return "http://127.0.0.1:11434";

        if (host.Equals("https://0.0.0.0", StringComparison.OrdinalIgnoreCase))
            return "https://127.0.0.1:11434";

        return host;
    }

    private static bool IsMatchingModel(JsonElement model, string expectedName)
    {
        string expected = NormalizeModelName(expectedName);

        if (model.TryGetProperty("name", out JsonElement name) &&
            NormalizeModelName(name.GetString()).Equals(expected, StringComparison.Ordinal))
        {
            return true;
        }

        if (model.TryGetProperty("model", out JsonElement modelProperty) &&
            NormalizeModelName(modelProperty.GetString()).Equals(expected, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeModelName(string? modelName) => (modelName ?? string.Empty).Trim().ToLowerInvariant();

    private static int? TryReadRuntimeContextFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                string name = prop.Name;
                if (LooksLikeContextKey(name) && TryReadPositiveInt(prop.Value, out int parsedFromKey))
                    return parsedFromKey;

                int? nested = TryReadRuntimeContextFromElement(prop.Value);
                if (nested is > 0)
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                int? nested = TryReadRuntimeContextFromElement(child);
                if (nested is > 0)
                    return nested;
            }
        }
        else if (TryReadPositiveInt(element, out int parsedDirect))
        {
            return parsedDirect;
        }

        return null;
    }

    private static bool LooksLikeContextKey(string key)
    {
        string normalized = key.ToLowerInvariant();
        return normalized is "num_ctx" or "context" or "context_length" or "n_ctx" or "ctx" ||
               normalized.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadContextFromModelInfo(JsonElement modelInfo, out int contextTokens)
    {
        contextTokens = 0;

        if (modelInfo.ValueKind != JsonValueKind.Object)
            return false;

        foreach (JsonProperty prop in modelInfo.EnumerateObject())
        {
            if (!prop.Name.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryReadPositiveInt(prop.Value, out contextTokens))
                return true;
        }

        return false;
    }

    private static bool TryReadContextFromParameters(JsonElement parameters, out int contextTokens)
    {
        contextTokens = 0;

        if (parameters.ValueKind == JsonValueKind.Object &&
            parameters.TryGetProperty("num_ctx", out JsonElement numCtx) &&
            TryReadPositiveInt(numCtx, out contextTokens))
        {
            return true;
        }

        if (parameters.ValueKind == JsonValueKind.String)
        {
            string raw = parameters.GetString() ?? string.Empty;
            Match match = Regex.Match(raw, @"\bnum_ctx\s+(\d+)\b", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed) && parsed > 0)
            {
                contextTokens = parsed;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadPositiveInt(JsonElement value, out int parsed)
    {
        parsed = 0;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int numeric) && numeric > 0)
        {
            parsed = numeric;
            return true;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out int fromString) && fromString > 0)
        {
            parsed = fromString;
            return true;
        }

        if (value.ValueKind == JsonValueKind.String && TryParseContextTokenValue(value.GetString(), out int contextLikeValue))
        {
            parsed = contextLikeValue;
            return true;
        }

        return false;
    }

    private static bool TryParseContextTokenValue(string? raw, out int parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string text = raw.Trim();

        if (text.Contains('/'))
        {
            string[] parts = text.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            text = parts.Length > 0 ? parts[^1] : text;
        }

        text = text.Replace(",", string.Empty, StringComparison.Ordinal);

        Match match = Regex.Match(text, @"^(?<value>\d+(\.\d+)?)\s*(?<suffix>[kKmM]?)$");
        if (match.Success)
        {
            double value = double.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
            string suffix = match.Groups["suffix"].Value;
            if (suffix.Equals("k", StringComparison.OrdinalIgnoreCase))
                value *= 1000;
            else if (suffix.Equals("m", StringComparison.OrdinalIgnoreCase))
                value *= 1_000_000;

            int rounded = (int)Math.Round(value);
            if (rounded > 0)
            {
                parsed = rounded;
                return true;
            }
        }

        return int.TryParse(text, out parsed) && parsed > 0;
    }
}