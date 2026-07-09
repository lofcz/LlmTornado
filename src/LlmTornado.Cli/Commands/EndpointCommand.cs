using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Code;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// /endpoint — list, add, or remove OpenAI-compatible endpoints (LM Studio, llama.cpp, vLLM).
/// </summary>
internal sealed class EndpointCommand : ICliCommand
{
    public string Name => "endpoint";
    public string Description => "Manage OpenAI-compatible endpoints (LM Studio / llama.cpp / vLLM)";
    public string Usage => "/endpoint [list | add <name> <base-url> [api-key] [context-tokens] | remove <name>]";

    private readonly AgentSettings _settings;
    private readonly ProviderDetectionResult _providers;
    private readonly CliAgentBuilder _builder;
    private readonly Func<Agents.DataModels.ChatRuntimeEvents, ValueTask> _runtimeEventHandler;
    private readonly Action<AgentSettings> _persistSettings;
    private readonly Action<string>? _onRefreshNotice;

    public EndpointCommand(
        AgentSettings settings,
        ProviderDetectionResult providers,
        CliAgentBuilder builder,
        Func<Agents.DataModels.ChatRuntimeEvents, ValueTask> runtimeEventHandler,
        Action<AgentSettings>? persistSettings = null,
        Action<string>? onRefreshNotice = null)
    {
        _settings = settings;
        _providers = providers;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
        _persistSettings = persistSettings ?? (s => CliStorage.SaveJson(CliStorage.SettingsPath, s));
        _onRefreshNotice = onRefreshNotice;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0 || args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            WriteList();
            return Task.FromResult(true);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "add" when args.Length >= 3:
                return Task.FromResult(Add(args[1], args[2],
                    args.Length >= 4 ? args[3] : null,
                    args.Length >= 5 && int.TryParse(args[4], out int ctx) ? ctx : (int?)null));

            case "remove" when args.Length >= 2:
                return Task.FromResult(Remove(args[1]));

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                return Task.FromResult(true);
        }
    }

    private void WriteList()
    {
        List<DetectedProvider> custom = _providers.Providers
            .Where(p => p.Provider == LLmProviders.Custom)
            .ToList();

        if (custom.Count == 0)
        {
            ConsoleRenderer.WriteInfo("No OpenAI-compatible endpoints registered.");
            ConsoleRenderer.WriteInfo("Add one: /endpoint add <name> <base-url> [api-key] [context-tokens]");
            ConsoleRenderer.WriteInfo("Or set TORNADO_OPENAI_COMPAT=name=url[|key][|ctx],...");
            return;
        }

        foreach (DetectedProvider provider in custom)
        {
            string label = provider.EndpointName ?? "custom";
            string ctx = provider.DefaultContextTokens is > 0
                ? $", default ctx {provider.DefaultContextTokens:N0}"
                : "";
            ConsoleRenderer.WriteInfo($"  [{label}] {provider.Models.Count} model(s){ctx}");
            foreach (ChatModel model in provider.Models)
            {
                string marker = model.Name == _builder.ActiveModel.Name ? " ← active" : "";
                string modelCtx = model.ContextTokens is > 0 ? $" (ctx {FormatK(model.ContextTokens.Value)})" : "";
                ConsoleRenderer.WriteInfo($"    {model.Name}{modelCtx}{marker}");
            }
        }
    }

    private bool Add(string name, string baseUrl, string? apiKey, int? contextTokens)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            ConsoleRenderer.WriteError("Endpoint name cannot be empty.");
            return true;
        }

        if (name.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleRenderer.WriteError("Name 'ollama' is reserved for the auto-detected Ollama host.");
            return true;
        }

        OpenAiCompatEndpoint endpoint = new()
        {
            Name = name,
            BaseUrl = OpenAiCompatEndpoint.NormalizeBaseUrl(baseUrl),
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            ContextTokens = contextTokens is > 0 ? contextTokens : null,
            Enabled = true,
        };

        List<ChatModel> models = OpenAiCompatProber.ProbeModels(endpoint, out string? warning);
        if (warning is not null)
            ConsoleRenderer.WriteWarning(warning);

        if (models.Count == 0)
        {
            ConsoleRenderer.WriteError(
                $"Could not list models at {endpoint.BaseUrl}/models. Endpoint was not saved.");
            return true;
        }

        // Persist
        _settings.OpenAiCompatEndpoints ??= [];
        int existing = _settings.OpenAiCompatEndpoints.FindIndex(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
            _settings.OpenAiCompatEndpoints[existing] = endpoint;
        else
            _settings.OpenAiCompatEndpoints.Add(endpoint);

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save settings: {ex.Message}");
            return true;
        }

        // Live-register into the current detection result
        TornadoApi dedicated = OpenAiCompatProber.CreateApi(endpoint);
        DetectedProvider? prior = _providers.Providers.FirstOrDefault(p =>
            p.EndpointName is not null &&
            p.EndpointName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (prior is not null)
            _providers.Providers.Remove(prior);

        _providers.Providers.Add(new DetectedProvider
        {
            Provider = LLmProviders.Custom,
            ApiKey = endpoint.ApiKey ?? string.Empty,
            Models = models,
            DefaultModel = models[0],
            EndpointName = endpoint.Name,
            DedicatedApi = dedicated,
            DefaultContextTokens = endpoint.ContextTokens,
        });

        ConsoleRenderer.WriteSuccess(
            $"Endpoint '{name}' added ({models.Count} model(s) at {endpoint.BaseUrl}). Use /model list.");
        _onRefreshNotice?.Invoke($"endpoint '{name}' added");
        return true;
    }

    private bool Remove(string name)
    {
        name = name.Trim();
        if (name.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            ConsoleRenderer.WriteError("Cannot remove the auto-detected Ollama endpoint. Stop Ollama or unset OLLAMA_HOST.");
            return true;
        }

        _settings.OpenAiCompatEndpoints ??= [];
        int removed = _settings.OpenAiCompatEndpoints.RemoveAll(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        DetectedProvider? live = _providers.Providers.FirstOrDefault(p =>
            p.EndpointName is not null &&
            p.EndpointName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (live is not null)
            _providers.Providers.Remove(live);

        if (removed == 0 && live is null)
        {
            ConsoleRenderer.WriteError($"Endpoint '{name}' not found.");
            return true;
        }

        try
        {
            _persistSettings(_settings);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError($"Failed to save settings: {ex.Message}");
            return true;
        }

        ConsoleRenderer.WriteSuccess($"Endpoint '{name}' removed.");
        return true;
    }

    private static string FormatK(int tokens) =>
        tokens >= 1000 ? $"{tokens / 1000.0:0.#}k" : tokens.ToString("N0");
}
