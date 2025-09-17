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
    /// Qwen3 is the latest generation of large language models in Qwen series.
    /// </summary>
    public static readonly ChatModel ModelQwen3235BA22B = new ChatModel("deepinfra-Qwen/Qwen3-235B-A22B", "Qwen/Qwen3-235B-A22B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3235BA22B"/>
    /// </summary>
    public readonly ChatModel Qwen3235BA22B = ModelQwen3235BA22B;
    
    /// <summary>
    /// Qwen3 is the latest generation of large language models in Qwen series, offering a comprehensive suite of dense..
    /// </summary>
    public static readonly ChatModel ModelQwen330BA3B = new ChatModel("deepinfra-Qwen3-30B-A3B", "Qwen3-30B-A3B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen330BA3B"/>
    /// </summary>
    public readonly ChatModel Qwen330BA3B = ModelQwen330BA3B;
    
    /// <summary>
    /// Qwen3 is the latest generation of large language models in Qwen series, offering a comprehensive suite of dense..
    /// </summary>
    public static readonly ChatModel ModelQwenQwen32B = new ChatModel("deepinfra-Qwen/Qwen3-32B", "Qwen/Qwen3-32B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwenQwen32B"/>
    /// </summary>
    public readonly ChatModel QwenQwen32B = ModelQwenQwen32B;
    
    /// <summary>
    /// Qwen3 is the latest generation of large language models in Qwen series, offering a comprehensive suite of dense and mixture..
    /// </summary>
    public static readonly ChatModel ModelQwenQwen14B = new ChatModel("deepinfra-Qwen/Qwen3-14B", "Qwen/Qwen3-14B", LLmProviders.DeepInfra, 40_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwenQwen14B"/>
    /// </summary>
    public readonly ChatModel QwenQwen14B = ModelQwenQwen14B;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelQwen3235BA22B, ModelQwen330BA3B, ModelQwenQwen32B, ModelQwenQwen14B]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraQwen()
    {

    }
}