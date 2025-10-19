using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Mistral models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraMistral : IVendorModelClassProvider
{
    /// <summary>
    /// Mistral-Small-3.2-24B-Instruct-2506 is an advanced multilingual instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3224BInstruct2506 = new ChatModel("deepinfra-mistralai/Mistral-Small-3.2-24B-Instruct-2506", "mistralai/Mistral-Small-3.2-24B-Instruct-2506", LLmProviders.DeepInfra, 125_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3224BInstruct2506"/>
    /// </summary>
    public readonly ChatModel MistralSmall3224BInstruct2506 = ModelMistralSmall3224BInstruct2506;
    
    /// <summary>
    /// Mistral-Small-3.1-24B-Instruct-2503 is a powerful multilingual instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3124BInstruct2503 = new ChatModel("deepinfra-mistralai/Mistral-Small-3.1-24B-Instruct-2503", "mistralai/Mistral-Small-3.1-24B-Instruct-2503", LLmProviders.DeepInfra, 125_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3124BInstruct2503"/>
    /// </summary>
    public readonly ChatModel MistralSmall3124BInstruct2503 = ModelMistralSmall3124BInstruct2503;
    
    /// <summary>
    /// Mistral-Small-24B-Instruct-2501 is an efficient multilingual instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMistralSmall24BInstruct2501 = new ChatModel("deepinfra-mistralai/Mistral-Small-24B-Instruct-2501", "mistralai/Mistral-Small-24B-Instruct-2501", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall24BInstruct2501"/>
    /// </summary>
    public readonly ChatModel MistralSmall24BInstruct2501 = ModelMistralSmall24BInstruct2501;
    
    /// <summary>
    /// Mistral-7B-Instruct-v0.3 is a compact and efficient instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMistral7BInstructV03 = new ChatModel("deepinfra-mistralai/Mistral-7B-Instruct-v0.3", "mistralai/Mistral-7B-Instruct-v0.3", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistral7BInstructV03"/>
    /// </summary>
    public readonly ChatModel Mistral7BInstructV03 = ModelMistral7BInstructV03;
    
    /// <summary>
    /// Mistral-Nemo-Instruct-2407 is a powerful 12B parameter model.
    /// </summary>
    public static readonly ChatModel ModelMistralNemoInstruct2407 = new ChatModel("deepinfra-mistralai/Mistral-Nemo-Instruct-2407", "mistralai/Mistral-Nemo-Instruct-2407", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralNemoInstruct2407"/>
    /// </summary>
    public readonly ChatModel MistralNemoInstruct2407 = ModelMistralNemoInstruct2407;
    
    /// <summary>
    /// Mixtral-8x7B-Instruct-v0.1 is a mixture of experts model with strong performance.
    /// </summary>
    public static readonly ChatModel ModelMixtral8x7BInstructV01 = new ChatModel("deepinfra-mistralai/Mixtral-8x7B-Instruct-v0.1", "mistralai/Mixtral-8x7B-Instruct-v0.1", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMixtral8x7BInstructV01"/>
    /// </summary>
    public readonly ChatModel Mixtral8x7BInstructV01 = ModelMixtral8x7BInstructV01;
    
    /// <summary>
    /// Devstral-Small-2507 is a coding-focused model optimized for software development.
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall2507 = new ChatModel("deepinfra-mistralai/Devstral-Small-2507", "mistralai/Devstral-Small-2507", LLmProviders.DeepInfra, 125_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall2507"/>
    /// </summary>
    public readonly ChatModel DevstralSmall2507 = ModelDevstralSmall2507;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelMistralSmall3224BInstruct2506, ModelMistralSmall3124BInstruct2503, ModelMistralSmall24BInstruct2501, ModelMistral7BInstructV03, ModelMistralNemoInstruct2407, ModelMixtral8x7BInstructV01, ModelDevstralSmall2507]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraMistral()
    {

    }
}

