using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 4.1 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude41 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest snapshot of Opus 4.1. Deprecated June 5, 2026; retired August 5, 2026 on the Claude API.
    /// </summary>
    [Obsolete("Retired August 5, 2026 on the Claude API. Use ChatModel.Anthropic.Claude48.Opus instead.")]
    public static readonly ChatModel ModelOpus250805 = new ChatModel("claude-opus-4-1-20250805", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus250805"/>
    /// </summary>
    [Obsolete("Retired August 5, 2026 on the Claude API. Use ChatModel.Anthropic.Claude48.Opus instead.")]
    public readonly ChatModel Opus250805 = ModelOpus250805;
    
    /// <summary>
    /// All known Claude 4.1 models from Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelOpus250805]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude41()
    {

    }
}