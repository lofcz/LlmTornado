using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Zai;

/// <summary>
/// GLM models from ZAI.
/// </summary>
public class ChatModelZaiGlm : IVendorModelClassProvider
{
    /// <summary>
    /// GLM-4.6 - Highest Performance, Strong Coding, More Versatile
    /// </summary>
    public static readonly ChatModel ModelGlm46 = new ChatModel("glm-4.6", LLmProviders.Zai, 200_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm46"/>
    /// </summary>
    public readonly ChatModel Glm46 = ModelGlm46;

    /// <summary>
    /// GLM-4.5 - Better Performance, Strong Reasoning, More Versatile
    /// </summary>
    public static readonly ChatModel ModelGlm45 = new ChatModel("glm-4.5", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45"/>
    /// </summary>
    public readonly ChatModel Glm45 = ModelGlm45;

    /// <summary>
    /// GLM-4.5V - Multimodal, Flexible Reasoning, State-of-the-art in its scale
    /// </summary>
    public static readonly ChatModel ModelGlm45V = new ChatModel("glm-4.5v", LLmProviders.Zai, 64_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45V"/>
    /// </summary>
    public readonly ChatModel Glm45V = ModelGlm45V;

    /// <summary>
    /// GLM-4.5-X - Good Performance, Strong Reasoning, Ultra-Fast Response
    /// </summary>
    public static readonly ChatModel ModelGlm45X = new ChatModel("glm-4.5-x", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45X"/>
    /// </summary>
    public readonly ChatModel Glm45X = ModelGlm45X;

    /// <summary>
    /// GLM-4.5-Air - Cost-Effective, Lightweight, High Performance
    /// </summary>
    public static readonly ChatModel ModelGlm45Air = new ChatModel("glm-4.5-air", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45Air"/>
    /// </summary>
    public readonly ChatModel Glm45Air = ModelGlm45Air;

    /// <summary>
    /// GLM-4.5-AirX - Lightweight, High Performance, Ultra-Fast Response
    /// </summary>
    public static readonly ChatModel ModelGlm45AirX = new ChatModel("glm-4.5-airx", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45AirX"/>
    /// </summary>
    public readonly ChatModel Glm45AirX = ModelGlm45AirX;

    /// <summary>
    /// GLM-4-32B-0414-128K - High intelligence at unmatched cost-efficiency
    /// </summary>
    public static readonly ChatModel ModelGlm432B = new ChatModel("glm-4-32b-0414-128k", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm432B"/>
    /// </summary>
    public readonly ChatModel Glm432B = ModelGlm432B;

    /// <summary>
    /// GLM-4.5-Flash - Lightweight, High Performance
    /// </summary>
    public static readonly ChatModel ModelGlm45Flash = new ChatModel("glm-4.5-flash", LLmProviders.Zai, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45Flash"/>
    /// </summary>
    public readonly ChatModel Glm45Flash = ModelGlm45Flash;

    /// <summary>
    /// All GLM models from ZAI.
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    /// <summary>
    /// All GLM models from ZAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGlm46,
        ModelGlm45,
        ModelGlm45V,
        ModelGlm45X,
        ModelGlm45Air,
        ModelGlm45AirX,
        ModelGlm432B,
        ModelGlm45Flash
    ]);

    internal ChatModelZaiGlm()
    {
        
    }
}
