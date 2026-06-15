using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 4.7+ class models from Anthropic (NextOpus generation).
/// </summary>
public class ChatModelAnthropicClaude47 : IVendorModelClassProvider
{
    /// <summary>
    /// Claude Opus 4.7 - Next-generation Opus model with task budget support.
    /// Supports advisory token budgets for full agentic loops.
    /// </summary>
    public static readonly ChatModel ModelOpus = new ChatModel("claude-opus-4-7", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus"/>
    /// </summary>
    public readonly ChatModel Opus = ModelOpus;
    
    /// <summary>
    /// Claude Opus 4.8 - Latest Opus model with task budget support.
    /// Supports advisory token budgets for full agentic loops.
    /// </summary>
    public static readonly ChatModel ModelOpus48 = new ChatModel("claude-opus-4-8", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus48"/>
    /// </summary>
    public readonly ChatModel Opus48 = ModelOpus48;

    /// <summary>
    /// NextOpus (Claude Opus 4.8) research preview. Alias for <see cref="Opus48"/>.
    /// </summary>
    public readonly ChatModel NextOpus = ModelOpus48;
    
    /// <summary>
    /// All known Claude 4.7+ models from Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelOpus, ModelOpus48
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude47()
    {
    }
}
