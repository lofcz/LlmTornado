using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude37 : IVendorModelClassProvider
{
    /// <summary>
    /// Hybrid reasoning model.
    /// </summary>
    public static readonly ChatModel ModelSonnet = new ChatModel("claude-3-7-sonnet-20250219", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// Latest snapshot of Sonnet 3.7
    /// </summary>
    public static readonly ChatModel ModelSonnetLatest = new ChatModel("claude-3-7-sonnet-latest", LLmProviders.Anthropic, 200_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Sonnet = ModelSonnet;
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnetLatest"/>
    /// </summary>
    public readonly ChatModel SonnetLatest = ModelSonnetLatest;
    
    /// <summary>
    /// All known Claude 3.7 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelSonnet,
        ModelSonnetLatest
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude37()
    {

    }
}