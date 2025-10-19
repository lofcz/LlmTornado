using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Other models available on Blablador.
/// </summary>
public class ChatModelBlabladorOthers : IVendorModelClassProvider
{
    /// <summary>
    /// Phi-4-multimodal-instruct - Microsoft's Phi-4 with multimodal capabilities.
    /// </summary>
    public static readonly ChatModel ModelPhi4MultimodalInstruct = new ChatModel("Phi-4-multimodal-instruct", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelPhi4MultimodalInstruct"/>
    /// </summary>
    public readonly ChatModel Phi4MultimodalInstruct = ModelPhi4MultimodalInstruct;
    
    /// <summary>
    /// Tongyi-DeepResearch-30B-A3B - Alibaba's Tongyi deep research model with 30B parameters.
    /// </summary>
    public static readonly ChatModel ModelTongyiDeepResearch30BA3B = new ChatModel("Tongyi-DeepResearch-30B-A3B", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelTongyiDeepResearch30BA3B"/>
    /// </summary>
    public readonly ChatModel TongyiDeepResearch30BA3B = ModelTongyiDeepResearch30BA3B;
    
    /// <summary>
    /// facebook-cwm - Facebook's CWM (Context Window Model).
    /// </summary>
    public static readonly ChatModel ModelFacebookCwm = new ChatModel("facebook-cwm", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelFacebookCwm"/>
    /// </summary>
    public readonly ChatModel FacebookCwm = ModelFacebookCwm;
    
    /// <summary>
    /// All other models from Blablador.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelPhi4MultimodalInstruct, ModelTongyiDeepResearch30BA3B, ModelFacebookCwm
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelBlabladorOthers()
    {

    }
}

