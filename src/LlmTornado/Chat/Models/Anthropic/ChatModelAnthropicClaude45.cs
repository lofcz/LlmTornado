using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 4.5 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude45 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest snapshot of Haiku 4.5
    /// </summary>
    public static readonly ChatModel ModelHaiku251001 = new ChatModel("claude-haiku-4-5-20251001", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelHaiku251001"/>
    /// </summary>
    public readonly ChatModel Haiku251001 = ModelHaiku251001;
    
    /// <summary>
    /// Latest snapshot of Sonnet 4.5
    /// </summary>
    public static readonly ChatModel ModelSonnet250929 = new ChatModel("claude-sonnet-4-5-20250929", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnet250929"/>
    /// </summary>
    public readonly ChatModel Sonnet250929 = ModelSonnet250929;
    
    /// <summary>
    /// All known Claude 4.5 models from Anthropic.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelSonnet250929, ModelHaiku251001
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude45()
    {

    }
}
