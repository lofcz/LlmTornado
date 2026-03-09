using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 4.6 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude46 : IVendorModelClassProvider
{
    /// <summary>
    /// Claude Sonnet 4.6 - Balanced model combining speed and intelligence for everyday tasks.
    /// Features improved agentic search performance, extended thinking (manual and adaptive),
    /// interleaved thinking, and context awareness.
    /// Supports a 200K context window (1M available in beta).
    /// Alias: <c>claude-sonnet-4-6</c>.
    /// </summary>
    public static readonly ChatModel ModelSonnet = new ChatModel("claude-sonnet-4-6", LLmProviders.Anthropic, 200_000, [ "claude-sonnet-4-6-20260217" ]);

    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Sonnet = ModelSonnet;

    /// <summary>
    /// Claude Opus 4.6 - The most intelligent model for building agents and coding.
    /// Features adaptive thinking, 128K output tokens, compaction API, and data residency controls.
    /// Supports a 200K context window (1M available in beta).
    /// </summary>
    public static readonly ChatModel ModelOpus = new ChatModel("claude-opus-4-6", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus"/>
    /// </summary>
    public readonly ChatModel Opus = ModelOpus;
    
    /// <summary>
    /// All known Claude 4.6 models from Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelSonnet, ModelOpus
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude46()
    {

    }
}
