using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Code;

namespace LlmTornado.Cli.Commands;

internal sealed class ModelCommand : ICliCommand
{
    public string Name => "model";
    public string Description => "View and switch LLM models";
    public string Usage => "/model [list | info | set <model-name>]";

    private readonly ProviderDetectionResult _providers;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public ModelCommand(ProviderDetectionResult providers, CliAgentBuilder builder, Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _providers = providers;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public async Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Active model: {_builder.ActiveModel.Name}");
            return true;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                foreach (DetectedProvider provider in _providers.Providers)
                {
                    ConsoleRenderer.WriteInfo($"\n  {provider.Provider}:");
                    foreach (ChatModel model in provider.Models)
                    {
                        string marker = model.Name == _builder.ActiveModel.Name ? " ← active" : "";
                        ConsoleRenderer.WriteInfo($"    {model.Name}{marker}");
                    }
                }
                break;

            case "info":
                await WriteModelInfo();
                break;

            case "set" when args.Length >= 2:
                string modelName = args[1];
                ChatModel? found = _providers.AllModels
                    .FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
                if (found is null)
                {
                    ConsoleRenderer.WriteError($"Model '{modelName}' not found. Use /model list.");
                }
                else
                {
                    if (found.Provider == LLmProviders.Custom)
                    {
                        string ollamaHost = OllamaContextInspector.ResolveHost(Environment.GetEnvironmentVariable("OLLAMA_HOST"));
                        int? runtimeContext = await OllamaContextInspector.TryGetRuntimeContextTokens(found.Name, ollamaHost);
                        int? modelCardContext = runtimeContext is > 0
                            ? null
                            : await OllamaContextInspector.TryGetModelCardContextTokens(found.Name, ollamaHost);
                        int? detectedContext = runtimeContext ?? modelCardContext;

                        if (detectedContext is > 0)
                        {
                            found = new ChatModel(found.Name, found.Provider, detectedContext.Value);

                            if (runtimeContext is > 0)
                                ConsoleRenderer.WriteInfo($"Detected Ollama runtime context: {detectedContext:N0} tokens");
                            else
                                ConsoleRenderer.WriteInfo($"Detected Ollama model context: {detectedContext:N0} tokens");
                        }
                        else if (found.ContextTokens is null)
                        {
                            ConsoleRenderer.WriteInfo("Could not detect Ollama context size; compression will use the default budget.");
                        }
                    }

                    _builder.SetModel(found, _runtimeEventHandler);
                    ConsoleRenderer.WriteSuccess($"Switched to: {found.Name}");
                }
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return true;
    }

    private async Task WriteModelInfo()
    {
        ChatModel activeModel = _builder.ActiveModel;
        ConsoleRenderer.WriteInfo($"Active model: {activeModel.Name}");
        ConsoleRenderer.WriteInfo($"Provider: {activeModel.Provider}");

        if (activeModel.Provider != LLmProviders.Custom)
        {
            WriteContextInfo(activeModel.ContextTokens, "Context window", "Context window is unknown for this model.");
            return;
        }

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
    }

    private static void WriteContextInfo(int? contextTokens, string label, string unknownMessage)
    {
        if (contextTokens is > 0)
        {
            ConsoleRenderer.WriteSuccess($"{label}: {contextTokens:N0} tokens");
        }
        else
        {
            ConsoleRenderer.WriteInfo(unknownMessage);
        }
    }
}
