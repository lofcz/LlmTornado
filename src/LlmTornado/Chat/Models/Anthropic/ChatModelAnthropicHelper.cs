using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Shared capability checks for Anthropic Claude Opus 4.7+ models.
/// </summary>
public static class ChatModelAnthropicHelper
{
    /// <summary>
    /// Default context window on Claude Opus 4.7+ (1M tokens, no beta header).
    /// </summary>
    public const int Opus47PlusContextTokens = 1_000_000;

    /// <summary>
    /// Maximum output tokens on Claude Opus 4.7+.
    /// </summary>
    public const int Opus47PlusMaxOutputTokens = 128_000;

    /// <summary>
    /// High-resolution vision long-edge limit in pixels on Claude Opus 4.7+.
    /// </summary>
    public const int Opus47PlusHighResVisionLongEdgePx = 2576;

    /// <summary>
    /// API model ID for Claude Opus 4.8 (internal codename: NextOpus).
    /// </summary>
    public const string Opus48ModelId = "claude-opus-4-8";

    /// <summary>
    /// API model ID for Claude Opus 4.7.
    /// </summary>
    public const string Opus47ModelId = "claude-opus-4-7";

    /// <summary>
    /// API model ID for Claude Fable 5.
    /// </summary>
    public const string Fable5ModelId = "claude-fable-5";

    /// <summary>
    /// API model ID for Claude Sonnet 5.
    /// </summary>
    public const string Sonnet5ModelId = "claude-sonnet-5";

    /// <summary>
    /// Returns true for <c>claude-opus-4-7</c> and <c>claude-opus-4-8</c> (and aliases resolving to them).
    /// </summary>
    public static bool IsOpus47OrNewer(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith(Opus47ModelId, StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith(Opus48ModelId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true for Claude 5 generation models (<c>claude-fable-5</c>, <c>claude-sonnet-5</c>).
    /// </summary>
    public static bool IsClaude5Model(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith(Fable5ModelId, StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith(Sonnet5ModelId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true for models that only support adaptive thinking (Opus 4.7+ and Claude 5 generation).
    /// These models reject manual extended thinking and require adaptive thinking when thinking is enabled.
    /// </summary>
    public static bool IsAdaptiveOnlyThinkingModel(string? modelName)
        => IsOpus47OrNewer(modelName) || IsClaude5Model(modelName);

    /// <summary>
    /// Returns true for Claude Opus 4.6 and newer Opus generations.
    /// </summary>
    public static bool IsOpus46OrNewer(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith("claude-opus-4-6", StringComparison.OrdinalIgnoreCase)
            || IsOpus47OrNewer(modelName);
    }

    /// <summary>
    /// Returns true when <c>thinking.type = "adaptive"</c> is supported (Opus 4.6+, Sonnet 4.6, Opus 4.7/4.8, Claude 5).
    /// On Opus 4.7+ and Claude 5 manual <c>thinking.type = "enabled"</c> is upgraded to adaptive automatically.
    /// On Claude Fable 5 adaptive thinking is always on and <c>thinking.type = "disabled"</c> is not supported.
    /// </summary>
    public static bool SupportsAdaptiveThinking(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith("claude-opus-4-6", StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase)
            || IsOpus47OrNewer(modelName)
            || IsClaude5Model(modelName);
    }

    /// <summary>
    /// Returns true when the effort parameter is supported (serialized to <c>output_config.effort</c>).
    /// Claude 5 models default to <c>high</c> effort on the Claude API and Claude Code.
    /// </summary>
    public static bool IsEffortCompatibleModel(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith("claude-opus-4-5", StringComparison.OrdinalIgnoreCase)
            || IsOpus46OrNewer(modelName)
            || modelName.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase)
            || IsClaude5Model(modelName);
    }

    /// <summary>
    /// Models that reject non-default <c>temperature</c>, <c>top_p</c>, and <c>top_k</c> with HTTP 400.
    /// Applies to Claude Opus 4.7+ and Claude 5 generation (adaptive-only thinking models).
    /// </summary>
    public static bool RejectsNonDefaultSamplingParams(string? modelName) => IsAdaptiveOnlyThinkingModel(modelName);

    /// <summary>
    /// Models that reject manual extended thinking (<c>thinking.type = enabled</c> with <c>budget_tokens</c>).
    /// Applies to Claude Opus 4.7+ and Claude 5 generation (adaptive-only thinking models).
    /// </summary>
    public static bool RejectsManualExtendedThinking(string? modelName) => IsAdaptiveOnlyThinkingModel(modelName);

    /// <summary>
    /// Models that only support adaptive thinking when thinking is enabled.
    /// Applies to Claude Opus 4.7+ and Claude 5 generation.
    /// </summary>
    public static bool RequiresAdaptiveThinkingWhenEnabled(string? modelName) => IsAdaptiveOnlyThinkingModel(modelName);

    /// <summary>
    /// High-resolution vision (up to 2576px long edge) is automatic on Claude Opus 4.7+ and Claude 5.
    /// </summary>
    public static bool SupportsHighResVision(string? modelName) => IsAdaptiveOnlyThinkingModel(modelName);

    /// <summary>
    /// Known adaptive-only thinking chat models (Opus 4.7+ and Claude 5) for policy checks keyed by <see cref="IModel.Name"/>.
    /// </summary>
    internal static HashSet<IModel> Opus47PlusModels => LazyOpus47PlusModels.Value;

    private static readonly Lazy<HashSet<IModel>> LazyOpus47PlusModels = new Lazy<HashSet<IModel>>(() =>
    [
        ChatModelAnthropicClaude47.ModelOpus,
        ChatModelAnthropicClaude48.ModelOpus,
        ChatModelAnthropicClaude48.ModelNextOpus,
        ChatModelAnthropicClaude5.ModelFable,
        ChatModelAnthropicClaude5.ModelSonnet
    ]);

    /// <summary>
    /// Strips sampling parameters that cause HTTP 400 on Claude Opus 4.7+.
    /// </summary>
    public static void ClearSamplingParamsIfUnsupported(ChatRequest request)
    {
        if (request.Model is null || !RejectsNonDefaultSamplingParams(request.Model.Name))
        {
            return;
        }

        request.Temperature = null;
        request.TopP = null;
    }
}
