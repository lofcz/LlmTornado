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
    /// DeepSeek-V3.2-Exp is an experimental version of DeepSeek's advanced AI system.
    /// </summary>
    public static readonly ChatModel ModelV32Exp = new ChatModel("deepinfra-deepseek-ai/DeepSeek-V3.2-Exp", "deepseek-ai/DeepSeek-V3.2-Exp", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV32Exp"/>
    /// </summary>
    public readonly ChatModel V32Exp = ModelV32Exp;
    
    /// <summary>
    /// DeepSeek-V3.1-Terminus is an advanced iteration of the DeepSeek AI system.
    /// </summary>
    public static readonly ChatModel ModelV31Terminus = new ChatModel("deepinfra-deepseek-ai/DeepSeek-V3.1-Terminus", "deepseek-ai/DeepSeek-V3.1-Terminus", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV31Terminus"/>
    /// </summary>
    public readonly ChatModel V31Terminus = ModelV31Terminus;
    
    /// <summary>
    /// DeepSeek-V3.1 is a powerful AI system from DeepSeek.
    /// </summary>
    public static readonly ChatModel ModelV31 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-V3.1", "deepseek-ai/DeepSeek-V3.1", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV31"/>
    /// </summary>
    public readonly ChatModel V31 = ModelV31;
    
    /// <summary>
    /// DeepSeek-V3 is a strong Mixture-of-Experts (MoE) language model.
    /// </summary>
    public static readonly ChatModel ModelV3 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-V3", "deepseek-ai/DeepSeek-V3", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    public readonly ChatModel V3 = ModelV3;
    
    /// <summary>
    /// We introduce DeepSeek-R1, which incorporates cold-start data before RL. DeepSeek-R1 achieves performance comparable to OpenAI-o1.
    /// </summary>
    public static readonly ChatModel ModelR1 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1", "deepseek-ai/DeepSeek-R1", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1"/>
    /// </summary>
    public readonly ChatModel R1 = ModelR1;
    
    /// <summary>
    /// The DeepSeek R1 model has undergone a minor version upgrade, with the current version being DeepSeek-R1-0528.
    /// </summary>
    public static readonly ChatModel ModelR10528 = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-0528", "deepseek-ai/DeepSeek-R1-0528", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR10528"/>
    /// </summary>
    public readonly ChatModel R10528 = ModelR10528;
    
    /// <summary>
    /// DeepSeek-R1-Turbo is a faster variant of the DeepSeek-R1 model.
    /// </summary>
    public static readonly ChatModel ModelR1Turbo = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-Turbo", "deepseek-ai/DeepSeek-R1-Turbo", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1Turbo"/>
    /// </summary>
    public readonly ChatModel R1Turbo = ModelR1Turbo;
    
    /// <summary>
    /// DeepSeek-R1-0528-Turbo is a faster variant of the DeepSeek-R1-0528 model.
    /// </summary>
    public static readonly ChatModel ModelR10528Turbo = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-0528-Turbo", "deepseek-ai/DeepSeek-R1-0528-Turbo", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR10528Turbo"/>
    /// </summary>
    public readonly ChatModel R10528Turbo = ModelR10528Turbo;
    
    /// <summary>
    /// DeepSeek-Prover-V2, an open-source large language model designed for formal theorem proving in Lean 4.
    /// </summary>
    public static readonly ChatModel ModelProverV2671B = new ChatModel("deepinfra-deepseek-ai/DeepSeek-Prover-V2-671B", "deepseek-ai/DeepSeek-Prover-V2-671B", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelProverV2671B"/>
    /// </summary>
    public readonly ChatModel ProverV2671B = ModelProverV2671B;
    
    /// <summary>
    /// DeepSeek-R1-Distill-Llama-70B is a distilled version of DeepSeek-R1 based on Llama architecture.
    /// </summary>
    public static readonly ChatModel ModelR1DistillLlama70B = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-Distill-Llama-70B", "deepseek-ai/DeepSeek-R1-Distill-Llama-70B", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1DistillLlama70B"/>
    /// </summary>
    public readonly ChatModel R1DistillLlama70B = ModelR1DistillLlama70B;
    
    /// <summary>
    /// DeepSeek-R1-Distill-Qwen-32B is a distilled version of DeepSeek-R1 based on Qwen architecture.
    /// </summary>
    public static readonly ChatModel ModelR1DistillQwen32B = new ChatModel("deepinfra-deepseek-ai/DeepSeek-R1-Distill-Qwen-32B", "deepseek-ai/DeepSeek-R1-Distill-Qwen-32B", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelR1DistillQwen32B"/>
    /// </summary>
    public readonly ChatModel R1DistillQwen32B = ModelR1DistillQwen32B;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelV32Exp, ModelV31Terminus, ModelV31, ModelV3, ModelR1, ModelR10528, ModelR1Turbo, ModelR10528Turbo, ModelProverV2671B, ModelR1DistillLlama70B, ModelR1DistillQwen32B]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraDeepSeek()
    {

    }
}