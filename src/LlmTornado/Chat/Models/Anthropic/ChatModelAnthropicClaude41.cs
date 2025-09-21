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
    /// Latest snapshot of Opus 4.1
    /// </summary>
    public static readonly ChatModel ModelOpus250805 = new ChatModel("claude-opus-4-1-20250805", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus250805"/>
    /// </summary>
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