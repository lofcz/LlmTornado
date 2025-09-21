using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Microsoft models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraMicrosoft : IVendorModelClassProvider
{
    /// <summary>
    /// The Llama 4 collection of models are natively multimodal AI models that enable text and multimodal experiences.
    /// </summary>
    public static readonly ChatModel ModelPhi4ReasoningPlus = new ChatModel("deepinfra-microsoft/phi-4-reasoning-plus", "microsoft/phi-4-reasoning-plus", LLmProviders.DeepInfra, 1_024_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPhi4ReasoningPlus"/>
    /// </summary>
    public readonly ChatModel Phi4ReasoningPlus = ModelPhi4ReasoningPlus;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelPhi4ReasoningPlus]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraMicrosoft()
    {

    }
}