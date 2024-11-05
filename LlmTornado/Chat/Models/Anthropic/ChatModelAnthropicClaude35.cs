using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude35 : IVendorModelClassProvider
{
    /// <summary>
    /// Most balanced model between intelligence and speed, a great choice for enterprise workloads and scaled AI deployments.
    /// </summary>
    public static readonly ChatModel ModelSonnet = new ChatModel("claude-3-5-sonnet-20240620", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// New snapshot of Sonnet 3.5.
    /// </summary>
    public static readonly ChatModel ModelSonnet241022 = new ChatModel("claude-3-5-sonnet-20241022", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// Points to <see cref="ModelSonnet241022"/>.
    /// </summary>
    public static readonly ChatModel ModelSonnetLatest = new ChatModel("claude-3-5-sonnet-latest", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// Fastest and most compact model, designed for near-instant responsiveness and seamless AI experiences that mimic human interactions. 4x pricier than Haiku 3.
    /// </summary>
    public static readonly ChatModel ModelHaiku = new ChatModel("claude-3-5-haiku-20241022", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelHaiku"/>
    /// </summary>
    public readonly ChatModel Haiku = ModelHaiku;
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Sonnet = ModelSonnet;
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnet241022"/>
    /// </summary>
    public readonly ChatModel Sonnet241022 = ModelSonnet241022;
    
    /// <summary>
    /// <inheritdoc cref="ModelSonnetLatest"/>
    /// </summary>
    public readonly ChatModel SonnetLatest = ModelSonnetLatest;
    
    /// <summary>
    /// All known Claude 3 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelSonnet,
        ModelSonnet241022,
        ModelSonnetLatest,
        ModelHaiku
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude35()
    {

    }
}