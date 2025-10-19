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
    /// Phi-4 is a cost-effective, high-performance AI model.
    /// </summary>
    public static readonly ChatModel ModelPhi4 = new ChatModel("deepinfra-microsoft/phi-4", "microsoft/phi-4", LLmProviders.DeepInfra, 16_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPhi4"/>
    /// </summary>
    public readonly ChatModel Phi4 = ModelPhi4;
    
    /// <summary>
    /// Phi-4-multimodal-instruct is a multimodal instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelPhi4MultimodalInstruct = new ChatModel("deepinfra-microsoft/Phi-4-multimodal-instruct", "microsoft/Phi-4-multimodal-instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPhi4MultimodalInstruct"/>
    /// </summary>
    public readonly ChatModel Phi4MultimodalInstruct = ModelPhi4MultimodalInstruct;
    
    /// <summary>
    /// Phi-4-reasoning-plus is an advanced reasoning model from Microsoft.
    /// </summary>
    public static readonly ChatModel ModelPhi4ReasoningPlus = new ChatModel("deepinfra-microsoft/phi-4-reasoning-plus", "microsoft/phi-4-reasoning-plus", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPhi4ReasoningPlus"/>
    /// </summary>
    public readonly ChatModel Phi4ReasoningPlus = ModelPhi4ReasoningPlus;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelPhi4, ModelPhi4MultimodalInstruct, ModelPhi4ReasoningPlus]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraMicrosoft()
    {

    }
}