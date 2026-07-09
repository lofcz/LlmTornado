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
    /// Returns true for Claude 5 models (<c>claude-fable-5</c>, <c>claude-sonnet-5</c>).
    /// </summary>
    public static bool IsClaude5(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith(Fable5ModelId, StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith(Sonnet5ModelId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true for <c>claude-opus-4-7</c>, <c>claude-opus-4-8</c>, and Claude 5 models.
    /// </summary>
    public static bool IsOpus47OrNewer(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith(Opus47ModelId, StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith(Opus48ModelId, StringComparison.OrdinalIgnoreCase)
            || IsClaude5(modelName);
    }

    /// <summary>
    /// Returns true for Claude Opus 4.6 and newer Opus generations (including Claude 5).
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
    /// On Opus 4.7+ / Claude 5, manual <c>thinking.type = "enabled"</c> is upgraded to adaptive automatically.
    /// </summary>
    public static bool SupportsAdaptiveThinking(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith("claude-opus-4-6", StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase)
            || IsOpus47OrNewer(modelName);
    }

    /// <summary>
    /// Returns true when the effort parameter is supported (serialized to <c>output_config.effort</c>).
    /// </summary>
    public static bool IsEffortCompatibleModel(string? modelName)
    {
        if (modelName is null)
        {
            return false;
        }

        return modelName.StartsWith("claude-opus-4-5", StringComparison.OrdinalIgnoreCase)
            || IsOpus46OrNewer(modelName)
            || modelName.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Claude Opus 4.7+ / Claude 5 reject non-default <c>temperature</c>, <c>top_p</c>, and <c>top_k</c> with HTTP 400.
    /// </summary>
    public static bool RejectsNonDefaultSamplingParams(string? modelName) => IsOpus47OrNewer(modelName);

    /// <summary>
    /// Claude Opus 4.7+ / Claude 5 reject manual extended thinking (<c>thinking.type = enabled</c> with <c>budget_tokens</c>).
    /// </summary>
    public static bool RejectsManualExtendedThinking(string? modelName) => IsOpus47OrNewer(modelName);

    /// <summary>
    /// Claude Opus 4.7+ / Claude 5 only support adaptive thinking when thinking is enabled.
    /// </summary>
    public static bool RequiresAdaptiveThinkingWhenEnabled(string? modelName) => IsOpus47OrNewer(modelName);

    /// <summary>
    /// High-resolution vision (up to 2576px long edge) is automatic on Claude Opus 4.7+ / Claude 5.
    /// </summary>
    public static bool SupportsHighResVision(string? modelName) => IsOpus47OrNewer(modelName);

    /// <summary>
    /// Known Opus 4.7+ / Claude 5 chat models for policy checks keyed by <see cref="IModel.Name"/>.
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
