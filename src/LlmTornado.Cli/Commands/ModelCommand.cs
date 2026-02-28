using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Providers;

namespace LlmTornado.Cli.Commands;

internal sealed class ModelCommand : ICliCommand
{
    public string Name => "model";
    public string Description => "View and switch LLM models";
    public string Usage => "/model [list | set <model-name>]";

    private readonly ProviderDetectionResult _providers;
    private readonly CliAgentBuilder _builder;
    private readonly Func<ChatRuntimeEvents, ValueTask> _runtimeEventHandler;

    public ModelCommand(ProviderDetectionResult providers, CliAgentBuilder builder, Func<ChatRuntimeEvents, ValueTask> runtimeEventHandler)
    {
        _providers = providers;
        _builder = builder;
        _runtimeEventHandler = runtimeEventHandler;
    }

    public Task<bool> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleRenderer.WriteInfo($"Active model: {_builder.ActiveModel.Name}");
            return Task.FromResult(true);
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
                    _builder.SetModel(found, _runtimeEventHandler);
                    ConsoleRenderer.WriteSuccess($"Switched to: {found.Name}");
                }
                break;

            default:
                ConsoleRenderer.WriteError($"Usage: {Usage}");
                break;
        }

        return Task.FromResult(true);
    }
}
