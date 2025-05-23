using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 4 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude4 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest snapshot of Sonnet 4
    /// </summary>
    public static readonly ChatModel ModelSonnet250514 = new ChatModel("claude-sonnet-4-20250514", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnet250514"/>
    /// </summary>
    public readonly ChatModel Sonnet250514 = ModelSonnet250514;
    
    /// <summary>
    /// Latest snapshot of Opus 4
    /// </summary>
    public static readonly ChatModel ModelOpus250514 = new ChatModel("claude-opus-4-20250514", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelOpus250514"/>
    /// </summary>
    public readonly ChatModel Opus250514 = ModelOpus250514;
    
    /// <summary>
    /// All known Claude 4 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelSonnet250514,
        ModelOpus250514
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude4()
    {

    }
}