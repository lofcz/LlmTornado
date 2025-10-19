using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Qwen models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraQwen : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen3-Coder-480B-A35B-Instruct-Turbo is a high-performance coding model with 480B parameters.
    /// </summary>
    public static readonly ChatModel ModelQwen3Coder480BA35BInstructTurbo = new ChatModel("deepinfra-Qwen/Qwen3-Coder-480B-A35B-Instruct-Turbo", "Qwen/Qwen3-Coder-480B-A35B-Instruct-Turbo", LLmProviders.DeepInfra, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3Coder480BA35BInstructTurbo"/>
    /// </summary>
    public readonly ChatModel Qwen3Coder480BA35BInstructTurbo = ModelQwen3Coder480BA35BInstructTurbo;
    
    /// <summary>
    /// Qwen3-Coder-480B-A35B-Instruct is a high-performance coding model with 480B parameters.
    /// </summary>
    public static readonly ChatModel ModelQwen3Coder480BA35BInstruct = new ChatModel("deepinfra-Qwen/Qwen3-Coder-480B-A35B-Instruct", "Qwen/Qwen3-Coder-480B-A35B-Instruct", LLmProviders.DeepInfra, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3Coder480BA35BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Coder480BA35BInstruct = ModelQwen3Coder480BA35BInstruct;
    
    /// <summary>
    /// Qwen3-235B-A22B-Thinking-2507 is a reasoning-focused model with advanced thinking capabilities.
    /// </summary>
    public static readonly ChatModel ModelQwen3235BA22BThinking2507 = new ChatModel("deepinfra-Qwen/Qwen3-235B-A22B-Thinking-2507", "Qwen/Qwen3-235B-A22B-Thinking-2507", LLmProviders.DeepInfra, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3235BA22BThinking2507"/>
    /// </summary>
    public readonly ChatModel Qwen3235BA22BThinking2507 = ModelQwen3235BA22BThinking2507;
    
    /// <summary>
    /// Qwen3-235B-A22B-Instruct-2507 is a high-performance instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelQwen3235BA22BInstruct2507 = new ChatModel("deepinfra-Qwen/Qwen3-235B-A22B-Instruct-2507", "Qwen/Qwen3-235B-A22B-Instruct-2507", LLmProviders.DeepInfra, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3235BA22BInstruct2507"/>
    /// </summary>
    public readonly ChatModel Qwen3235BA22BInstruct2507 = ModelQwen3235BA22BInstruct2507;
    
    /// <summary>
    /// QwQ-32B is a reasoning-focused model designed for question-answering.
    /// </summary>
    public static readonly ChatModel ModelQwQ32B = new ChatModel("deepinfra-Qwen/QwQ-32B", "Qwen/QwQ-32B", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwQ32B"/>
    /// </summary>
    public readonly ChatModel QwQ32B = ModelQwQ32B;
    
    /// <summary>
    /// Qwen3-235B-A22B is the latest generation of large language models in Qwen series.
    /// </summary>
    public static readonly ChatModel ModelQwen3235BA22B = new ChatModel("deepinfra-Qwen/Qwen3-235B-A22B", "Qwen/Qwen3-235B-A22B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3235BA22B"/>
    /// </summary>
    public readonly ChatModel Qwen3235BA22B = ModelQwen3235BA22B;
    
    /// <summary>
    /// Qwen3-32B is a high-performance model from the Qwen3 series.
    /// </summary>
    public static readonly ChatModel ModelQwen332B = new ChatModel("deepinfra-Qwen/Qwen3-32B", "Qwen/Qwen3-32B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen332B"/>
    /// </summary>
    public readonly ChatModel Qwen332B = ModelQwen332B;
    
    /// <summary>
    /// Qwen3-14B is a balanced model from the Qwen3 series.
    /// </summary>
    public static readonly ChatModel ModelQwen314B = new ChatModel("deepinfra-Qwen/Qwen3-14B", "Qwen/Qwen3-14B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen314B"/>
    /// </summary>
    public readonly ChatModel Qwen314B = ModelQwen314B;
    
    /// <summary>
    /// Qwen2.5-72B-Instruct is a powerful instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelQwen2572BInstruct = new ChatModel("deepinfra-Qwen/Qwen2.5-72B-Instruct", "Qwen/Qwen2.5-72B-Instruct", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen2572BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2572BInstruct = ModelQwen2572BInstruct;
    
    /// <summary>
    /// Qwen2.5-Coder-32B-Instruct is a coding-focused instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelQwen25Coder32BInstruct = new ChatModel("deepinfra-Qwen/Qwen2.5-Coder-32B-Instruct", "Qwen/Qwen2.5-Coder-32B-Instruct", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen25Coder32BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen25Coder32BInstruct = ModelQwen25Coder32BInstruct;
    
    /// <summary>
    /// Qwen2.5-7B-Instruct is an efficient instruction-following model.
    /// </summary>
    public static readonly ChatModel ModelQwen257BInstruct = new ChatModel("deepinfra-Qwen/Qwen2.5-7B-Instruct", "Qwen/Qwen2.5-7B-Instruct", LLmProviders.DeepInfra, 32_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen257BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen257BInstruct = ModelQwen257BInstruct;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelQwen3Coder480BA35BInstructTurbo, ModelQwen3Coder480BA35BInstruct, ModelQwen3235BA22BThinking2507, ModelQwen3235BA22BInstruct2507, ModelQwQ32B, ModelQwen3235BA22B, ModelQwen332B, ModelQwen314B, ModelQwen2572BInstruct, ModelQwen25Coder32BInstruct, ModelQwen257BInstruct]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraQwen()
    {

    }
}