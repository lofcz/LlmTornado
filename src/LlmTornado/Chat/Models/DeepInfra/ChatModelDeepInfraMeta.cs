using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Meta models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraMeta : IVendorModelClassProvider
{
    /// <summary>
    /// The Llama 4 Scout model is a natively multimodal AI model that enables text and multimodal experiences.
    /// </summary>
    public static readonly ChatModel ModelLlama4Scout17B16E = new ChatModel("deepinfra-meta-llama/Llama-4-Scout-17B-16E", "meta-llama/Llama-4-Scout-17B-16E", LLmProviders.DeepInfra, 320_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama4Scout17B16E"/>
    /// </summary>
    public readonly ChatModel Llama4Scout17B16E = ModelLlama4Scout17B16E;
    
    /// <summary>
    /// The Llama 4 Maverick model is a natively multimodal AI model with 128 experts that enables text and multimodal experiences.
    /// </summary>
    public static readonly ChatModel ModelLlama4Maverick17B128E = new ChatModel("deepinfra-meta-llama/Llama-4-Maverick-17B-128E", "meta-llama/Llama-4-Maverick-17B-128E", LLmProviders.DeepInfra, 1_024_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama4Maverick17B128E"/>
    /// </summary>
    public readonly ChatModel Llama4Maverick17B128E = ModelLlama4Maverick17B128E;
    
    /// <summary>
    /// The Llama 4 Maverick Turbo model is a faster variant optimized for low-latency applications.
    /// </summary>
    public static readonly ChatModel ModelLlama4Maverick17B128ETurbo = new ChatModel("deepinfra-meta-llama/Llama-4-Maverick-17B-128E-Turbo", "meta-llama/Llama-4-Maverick-17B-128E-Turbo", LLmProviders.DeepInfra, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama4Maverick17B128ETurbo"/>
    /// </summary>
    public readonly ChatModel Llama4Maverick17B128ETurbo = ModelLlama4Maverick17B128ETurbo;
    
    /// <summary>
    /// Llama Guard 4 is a safety model designed for content moderation.
    /// </summary>
    public static readonly ChatModel ModelLlamaGuard412B = new ChatModel("deepinfra-meta-llama/Llama-Guard-4-12B", "meta-llama/Llama-Guard-4-12B", LLmProviders.DeepInfra, 160_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlamaGuard412B"/>
    /// </summary>
    public readonly ChatModel LlamaGuard412B = ModelLlamaGuard412B;
    
    /// <summary>
    /// Llama 3.3 70B Instruct is a powerful instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelLlama3370BInstruct = new ChatModel("deepinfra-meta-llama/Llama-3.3-70B-Instruct", "meta-llama/Llama-3.3-70B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama3370BInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3370BInstruct = ModelLlama3370BInstruct;
    
    /// <summary>
    /// Llama 3.3 70B Instruct Turbo is a faster variant optimized for lower latency.
    /// </summary>
    public static readonly ChatModel ModelLlama3370BInstructTurbo = new ChatModel("deepinfra-meta-llama/Llama-3.3-70B-Instruct-Turbo", "meta-llama/Llama-3.3-70B-Instruct-Turbo", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama3370BInstructTurbo"/>
    /// </summary>
    public readonly ChatModel Llama3370BInstructTurbo = ModelLlama3370BInstructTurbo;
    
    /// <summary>
    /// Llama 3.2 11B Vision Instruct is a multimodal model with vision capabilities.
    /// </summary>
    public static readonly ChatModel ModelLlama3211BVisionInstruct = new ChatModel("deepinfra-meta-llama/Llama-3.2-11B-Vision-Instruct", "meta-llama/Llama-3.2-11B-Vision-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama3211BVisionInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3211BVisionInstruct = ModelLlama3211BVisionInstruct;
    
    /// <summary>
    /// Llama 3.2 3B Instruct is a compact instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelLlama323BInstruct = new ChatModel("deepinfra-meta-llama/Llama-3.2-3B-Instruct", "meta-llama/Llama-3.2-3B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama323BInstruct"/>
    /// </summary>
    public readonly ChatModel Llama323BInstruct = ModelLlama323BInstruct;
    
    /// <summary>
    /// Llama 3.2 1B Instruct is a highly efficient small instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelLlama321BInstruct = new ChatModel("deepinfra-meta-llama/Llama-3.2-1B-Instruct", "meta-llama/Llama-3.2-1B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama321BInstruct"/>
    /// </summary>
    public readonly ChatModel Llama321BInstruct = ModelLlama321BInstruct;
    
    /// <summary>
    /// Meta Llama 3.1 405B Instruct is a very large instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama31405BInstruct = new ChatModel("deepinfra-meta-llama/Meta-Llama-3.1-405B-Instruct", "meta-llama/Meta-Llama-3.1-405B-Instruct", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama31405BInstruct"/>
    /// </summary>
    public readonly ChatModel MetaLlama31405BInstruct = ModelMetaLlama31405BInstruct;
    
    /// <summary>
    /// Meta Llama 3.1 70B Instruct is a powerful instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama3170BInstruct = new ChatModel("deepinfra-meta-llama/Meta-Llama-3.1-70B-Instruct", "meta-llama/Meta-Llama-3.1-70B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama3170BInstruct"/>
    /// </summary>
    public readonly ChatModel MetaLlama3170BInstruct = ModelMetaLlama3170BInstruct;
    
    /// <summary>
    /// Meta Llama 3.1 70B Instruct Turbo is a faster variant optimized for lower latency.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama3170BInstructTurbo = new ChatModel("deepinfra-meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo", "meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama3170BInstructTurbo"/>
    /// </summary>
    public readonly ChatModel MetaLlama3170BInstructTurbo = ModelMetaLlama3170BInstructTurbo;
    
    /// <summary>
    /// Meta Llama 3.1 8B Instruct is a balanced instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama318BInstruct = new ChatModel("deepinfra-meta-llama/Meta-Llama-3.1-8B-Instruct", "meta-llama/Meta-Llama-3.1-8B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama318BInstruct"/>
    /// </summary>
    public readonly ChatModel MetaLlama318BInstruct = ModelMetaLlama318BInstruct;
    
    /// <summary>
    /// Meta Llama 3.1 8B Instruct Turbo is a faster variant optimized for lower latency.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama318BInstructTurbo = new ChatModel("deepinfra-meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo", "meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama318BInstructTurbo"/>
    /// </summary>
    public readonly ChatModel MetaLlama318BInstructTurbo = ModelMetaLlama318BInstructTurbo;
    
    /// <summary>
    /// Meta Llama 3 70B Instruct is a powerful instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama370BInstruct = new ChatModel("deepinfra-meta-llama/Meta-Llama-3-70B-Instruct", "meta-llama/Meta-Llama-3-70B-Instruct", LLmProviders.DeepInfra, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama370BInstruct"/>
    /// </summary>
    public readonly ChatModel MetaLlama370BInstruct = ModelMetaLlama370BInstruct;
    
    /// <summary>
    /// Meta Llama 3 8B Instruct is a balanced instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelMetaLlama38BInstruct = new ChatModel("deepinfra-meta-llama/Meta-Llama-3-8B-Instruct", "meta-llama/Meta-Llama-3-8B-Instruct", LLmProviders.DeepInfra, 8_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMetaLlama38BInstruct"/>
    /// </summary>
    public readonly ChatModel MetaLlama38BInstruct = ModelMetaLlama38BInstruct;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelLlama4Scout17B16E, ModelLlama4Maverick17B128E, ModelLlama4Maverick17B128ETurbo, ModelLlamaGuard412B, ModelLlama3370BInstruct, ModelLlama3370BInstructTurbo, ModelLlama3211BVisionInstruct, ModelLlama323BInstruct, ModelLlama321BInstruct, ModelMetaLlama31405BInstruct, ModelMetaLlama3170BInstruct, ModelMetaLlama3170BInstructTurbo, ModelMetaLlama318BInstruct, ModelMetaLlama318BInstructTurbo, ModelMetaLlama370BInstruct, ModelMetaLlama38BInstruct]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraMeta()
    {

    }
}