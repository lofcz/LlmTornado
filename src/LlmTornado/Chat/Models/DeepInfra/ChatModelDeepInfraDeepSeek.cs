using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// DeepSeek models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraDeepSeek : IVendorModelClassProvider
{
    /// <summary>
    /// The DeepSeek R1 model has undergone a minor version upgrade, with the current version being DeepSeek-R1-0528.
    /// </summary>
    public static readonly ChatModel ModelR10528 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-0528", "deepseek-ai/DeepSeek-R1-0528", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR10528"/>
    /// </summary>
    public readonly ChatModel R10528 = ModelR10528;
    
    /// <summary>
    /// DeepSeek-Prover-V2, an open-source large language model designed for formal theorem proving in Lean 4, with initialization data..
    /// </summary>
    public static readonly ChatModel ModelProverV2671B = new ChatModel("deepinfra-deepseek-ai/DeepSeek-Prover-V2-671B", "deepseek-ai/DeepSeek-Prover-V2-671B", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelProverV2671B"/>
    /// </summary>
    public readonly ChatModel ProverV2671B = ModelProverV2671B;
    
    /// <summary>
    /// We introduce DeepSeek-R1, which incorporates cold-start data before RL. DeepSeek-R1 achieves performance..
    /// </summary>
    public static readonly ChatModel ModelR1Turbo = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-Turbo", "deepseek-ai/DeepSeek-R1-Turbo", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1Turbo"/>
    /// </summary>
    public readonly ChatModel R1Turbo = ModelR1Turbo;
    
    /// <summary>
    /// We introduce DeepSeek-R1, which incorporates cold-start data before RL. DeepSeek-R1 achieves performance..
    /// </summary>
    public static readonly ChatModel ModelR1 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1", "deepseek-ai/DeepSeek-R1", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1"/>
    /// </summary>
    public readonly ChatModel R1 = ModelR1;

    /// <summary>
    /// DeepSeek-V3-0324, a strong Mixture-of-Experts (MoE) language model with 671B total parameters with 37B activated for each..
    /// </summary>
    public static readonly ChatModel ModelV30324 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-V3-0324", "deepseek-ai/DeepSeek-V3-0324", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV30324"/>
    /// </summary>
    public readonly ChatModel V30324 = ModelV30324;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelR10528, ModelProverV2671B, ModelR1Turbo, ModelR1, ModelV30324]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraDeepSeek()
    {

    }
}