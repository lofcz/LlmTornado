using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Agents;
using LlmTornado.Cli.Core.Interactions;
using LlmTornado.Cli.Core.Mcp;
using LlmTornado.Cli.Core.Skills;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Tools;

namespace LlmTornado.Cli;

/// <summary>
/// CLI-specific wrapper around Core's <see cref="AgentBuilder"/>.
/// Adds conversation memory management and console rendering on top of the shared builder.
/// </summary>
internal sealed class CliAgentBuilder
{
    private readonly AgentBuilder _inner;
    private readonly ConversationMemoryManager _memoryManager;

    public TornadoAgent Agent => _inner.Agent;
    public ChatRuntime Runtime => _inner.Runtime;
    public ChatModel ActiveModel => _inner.ActiveModel;
    public bool NeedsOptimization => _inner.NeedsOptimization;
    public int TotalToolCount => _inner.TotalToolCount;

    /// <summary>
    /// The managed conversation config that owns the per-turn sync/compress/persist lifecycle.
    /// </summary>
    public ManagedConversationRuntimeConfiguration? ConversationConfig => _inner.ConversationConfig;

    public CliAgentBuilder(
        TornadoApi api,
        ChatModel activeModel,
        SkillManager skillManager,
        McpConfigLoader mcpLoader,
        ToolApprovalManager toolApproval,
        ConversationMemoryManager memoryManager,
        AgentDefinitionManager agentManager,
        AgentSettings settings,
        ChatModel? optimizerModel)
    {
        _memoryManager = memoryManager;
        _inner = new AgentBuilder(
            api,
            activeModel,
            skillManager,
            mcpLoader,
            toolApproval,
            toolApproval,
            agentManager,
            settings,
            optimizerModel,
            additionalTools: null,
            memoryManager: memoryManager);
    }

    public ChatRuntime Build(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _inner.Build(onRuntimeEvent);
    }

    public ChatRuntime SetModel(ChatModel model, Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        _memoryManager.UpdateModel(model, model.ContextTokens);
        return _inner.SetModel(model, onRuntimeEvent);
    }

    public ChatRuntime RebuildForSkillChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _inner.RebuildForSkillChange(onRuntimeEvent);
    }

    public ChatRuntime RebuildForAgentChange(Func<ChatRuntimeEvents, ValueTask>? onRuntimeEvent = null)
    {
        return _inner.RebuildForAgentChange(onRuntimeEvent);
    }

    public async Task<ToolOptimizationResult?> OptimizeToolsForTurn(string userMessage, CancellationToken ct = default)
    {
        ToolOptimizationResult? result = await _inner.OptimizeToolsForTurn(userMessage, ct);

        if (result is not null)
        {
            if (result.WasOptimized)
                ConsoleRenderer.WriteToolOptimization(result.OriginalCount, result.SelectedCount);
            else if (result.FallbackReason is not null)
                ConsoleRenderer.WriteToolOptimizationSkipped(result.OriginalCount, result.FallbackReason);
        }

        return result;
    }

    public string WorkingDirectory => _inner.WorkingDirectory ?? Environment.CurrentDirectory;

    public void RestoreFullTools() => _inner.RestoreFullTools();

    public void SetOptimizerEnabled(bool enabled, ChatModel? optimizerModel = null)
        => _inner.SetOptimizerEnabled(enabled, optimizerModel);

    public void SetMaxTools(int maxTools, ChatModel? optimizerModel = null)
        => _inner.SetMaxTools(maxTools, optimizerModel);
}
