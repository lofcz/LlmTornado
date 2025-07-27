using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Claude 3 class models from Anthropic.
/// </summary>
public class ChatModelAnthropicClaude3 : IVendorModelClassProvider
{
    /// <summary>
    /// Fastest and most compact model, designed for near-instant responsiveness and seamless AI experiences that mimic human interactions.
    /// </summary>
    public static readonly ChatModel ModelHaiku = new ChatModel("claude-3-haiku-20240307", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelHaiku"/>
    /// </summary>
    public readonly ChatModel Haiku = ModelHaiku;

    /// <summary>
    /// Most balanced model between intelligence and speed, a great choice for enterprise workloads and scaled AI deployments.
    /// </summary>
    [Obsolete("deprecated")]
    public static readonly ChatModel ModelSonnet = new ChatModel("claude-3-sonnet-20240229", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    [Obsolete("deprecated")]
    public readonly ChatModel Sonnet = ModelSonnet;

    /// <summary>
    /// Most powerful model, delivering state-of-the-art performance on highly complex tasks and demonstrating fluency and human-like understanding.
    /// </summary>
    public static readonly ChatModel ModelOpus = new ChatModel("claude-3-opus-20240229", LLmProviders.Anthropic, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelSonnet"/>
    /// </summary>
    public readonly ChatModel Opus = ModelOpus;

    /// <summary>
    /// All known Claude 3 models from Anthropic.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelHaiku,
        ModelSonnet,
        ModelOpus
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAnthropicClaude3()
    {

    }
}