using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Code;

namespace LlmTornado.Cli.Commands;

internal sealed class ModelCommand : ICliCommand
{
    public string Name => "model";
    public string Description => "View and switch LLM models";
    public string Usage => "/model [list | info | set <model-name|endpoint/model> | refresh]";

    private readonly ProviderDetectionResult _providers;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;
    private readonly AgentSettings? _settings;
    private readonly Action<string>? _onWarning;

    public ModelCommand(
        ProviderDetectionResult providers,
        CliAgentBuilder builder,
        Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler,
        AgentSettings? settings = null,
        Action<string>? onWarning = null)
    {
        _providers = providers;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
        _settings = settings;
        _onWarning = onWarning;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            DetectedProvider? owner = _providers.FindOwner(_builder.ActiveModel);
            string endpoint = owner?.EndpointName is not null ? $" [{owner.EndpointName}]" : "";
            ConsoleRenderer.WriteInfo($"Active model: {_builder.ActiveModel.Name}{endpoint}");
            return true;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                WriteList();
                break;

            case "info":
                await WriteModelInfo();
                break;

            case "refresh":
                await RefreshAsync();
                break;

            case "set" when args.Length >= 2:
                await SetModelAsync(string.Join(' ', args.Skip(1)));
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
    }

    private void WriteList()
    {
        foreach (DetectedProvider provider in _providers.Providers)
        {
            string header = provider.EndpointName is not null
                ? $"[{provider.EndpointName}] ({provider.Provider})"
                : provider.Provider.ToString();
            ConsoleRenderer.WriteInfo($"\n  {header}:");
            foreach (ChatModel model in provider.Models)
            {
                string marker = model.Name == _builder.ActiveModel.Name &&
                                (provider.EndpointName is null ||
                                 ReferenceEquals(_providers.FindOwner(_builder.ActiveModel), provider))
                    ? " ← active"
                    : "";
                string ctx = model.ContextTokens is > 0
                    ? $" (ctx {FormatK(model.ContextTokens.Value)})"
                    : provider.DefaultContextTokens is > 0
                        ? $" (ctx ~{FormatK(provider.DefaultContextTokens.Value)})"
                        : "";
                ConsoleRenderer.WriteInfo($"    {model.Name}{ctx}{marker}");
            }
        }
    }

    private async Task SetModelAsync(string modelName)
    {
        ChatModel? found = _providers.ResolveModel(modelName, out string? ambiguity);
        if (ambiguity is not null)
        {
            ConsoleRenderer.WriteError(ambiguity);
            return;
        }

        if (found is null)
        {
            ConsoleRenderer.WriteError($"Model '{modelName}' not found. Use /model list.");
            return;
        }

        DetectedProvider? owner = _providers.FindOwner(found);
        found = await EnrichContextAsync(found, owner);

        TornadoApi api = _providers.GetApiForModel(found);
        _builder.SetModel(found, api, _runtimeEventHandler);

        string endpointLabel = owner?.EndpointName is not null ? $" [{owner.EndpointName}]" : "";
        ConsoleRenderer.WriteSuccess($"Switched to: {found.Name}{endpointLabel}");
    }

    private async Task<ChatModel> EnrichContextAsync(ChatModel model, DetectedProvider? owner)
    {
        if (model.Provider != LLmProviders.Custom)
            return model;

        // Ollama keeps its native context inspector.
        if (owner?.EndpointName is null or "ollama")
        {
            string ollamaHost = OllamaContextInspector.ResolveHost(Environment.GetEnvironmentVariable("OLLAMA_HOST"));
            int? runtimeContext = await OllamaContextInspector.TryGetRuntimeContextTokens(model.Name, ollamaHost);
            int? modelCardContext = runtimeContext is > 0
                ? null
                : await OllamaContextInspector.TryGetModelCardContextTokens(model.Name, ollamaHost);
            int? detectedContext = runtimeContext ?? modelCardContext;

            if (detectedContext is > 0)
            {
                if (runtimeContext is > 0)
                    ConsoleRenderer.WriteInfo($"Detected Ollama runtime context: {detectedContext:N0} tokens");
                else
                    ConsoleRenderer.WriteInfo($"Detected Ollama model context: {detectedContext:N0} tokens");
                return new ChatModel(model.Name, model.Provider, detectedContext.Value);
            }
        }

        if (model.ContextTokens is > 0)
            return model;

        int resolved = OpenAiCompatProber.ResolveContextTokens(
            model.ContextTokens,
            owner?.DefaultContextTokens,
            _settings?.CompressionContextTokenCap);

        if (model.ContextTokens is null)
        {
            ConsoleRenderer.WriteInfo(
                $"Context window unknown for '{model.Name}'; assuming {resolved:N0} tokens" +
                (owner?.DefaultContextTokens is > 0 ? " (endpoint default)." :
                    _settings?.CompressionContextTokenCap is > 0 ? " (compression cap)." : " (fallback)."));
        }

        return new ChatModel(model.Name, model.Provider, resolved);
    }

    private async Task RefreshAsync()
    {
        List<OpenAiCompatEndpoint> endpoints = OpenAiCompatEndpoint.Merge(
            _settings?.OpenAiCompatEndpoints,
            OpenAiCompatEndpoint.ParseEnv(Environment.GetEnvironmentVariable("TORNADO_OPENAI_COMPAT")));

        ProviderDetectionResult? refreshed = ProviderDetector.Detect(endpoints, warning =>
        {
            ConsoleRenderer.WriteWarning(warning);
            _onWarning?.Invoke(warning);
        });

        if (refreshed is null)
        {
            ConsoleRenderer.WriteError("Refresh found no providers.");
            return;
        }

        // Mutate the live result in place so other commands keep their reference.
        _providers.Providers.Clear();
        _providers.Providers.AddRange(refreshed.Providers);
        _providers.OptimizerModel = refreshed.OptimizerModel;

        // Keep active model if still present; otherwise leave as-is (user can /model set).
        ChatModel? stillThere = _providers.ResolveModel(_builder.ActiveModel.Name, out _);
        if (stillThere is not null)
            _providers.ActiveModel = stillThere;

        ConsoleRenderer.WriteSuccess(
            $"Refreshed: {_providers.Providers.Count} provider(s), {_providers.AllModels.Count} model(s).");
        await Task.CompletedTask;
    }

    private async Task WriteModelInfo()
    {
        ChatModel activeModel = _builder.ActiveModel;
        DetectedProvider? owner = _providers.FindOwner(activeModel);
        ConsoleRenderer.WriteInfo($"Active model: {activeModel.Name}");
        ConsoleRenderer.WriteInfo($"Provider: {activeModel.Provider}");
        if (owner?.EndpointName is not null)
            ConsoleRenderer.WriteInfo($"Endpoint: {owner.EndpointName}");

        if (activeModel.Provider != LLmProviders.Custom)
        {
            WriteContextInfo(activeModel.ContextTokens, "Context window", "Context window is unknown for this model.");
            return;
        }

        if (owner?.EndpointName is null or "ollama")
        {
            string ollamaHost = OllamaContextInspector.ResolveHost(Environment.GetEnvironmentVariable("OLLAMA_HOST"));
            int? runtimeContext = await OllamaContextInspector.TryGetRuntimeContextTokens(activeModel.Name, ollamaHost);
            int? modelCardContext = runtimeContext is > 0
                ? null
                : await OllamaContextInspector.TryGetModelCardContextTokens(activeModel.Name, ollamaHost);
            int? contextTokens = runtimeContext ?? modelCardContext ?? activeModel.ContextTokens;

            ConsoleRenderer.WriteInfo($"Ollama host: {ollamaHost}");
            if (runtimeContext is > 0)
            {
                WriteContextInfo(runtimeContext, "Ollama runtime context window (from running model)", "Runtime context size is unavailable.");
                return;
            }

            if (modelCardContext is > 0)
            {
                ConsoleRenderer.WriteInfo("Runtime context unavailable; using model metadata.");
                WriteContextInfo(modelCardContext, "Ollama model context window", "Model context size is unavailable.");
                return;
            }

            WriteContextInfo(contextTokens, "Ollama context window", "Unable to determine context window from Ollama runtime or model metadata.");
            return;
        }

        WriteContextInfo(
            activeModel.ContextTokens ?? owner.DefaultContextTokens,
            "Context window",
            "Context window is unknown for this endpoint model.");
    }

    private static void WriteContextInfo(int? contextTokens, string label, string unknownMessage)
    {
        if (contextTokens is > 0)
            ConsoleRenderer.WriteSuccess($"{label}: {contextTokens:N0} tokens");
        else
            ConsoleRenderer.WriteInfo(unknownMessage);
    }

    private static string FormatK(int tokens) =>
        tokens >= 1000 ? $"{tokens / 1000.0:0.#}k" : tokens.ToString("N0");
}
