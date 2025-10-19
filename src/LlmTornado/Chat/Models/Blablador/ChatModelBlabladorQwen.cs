using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Qwen family models available on Blablador.
/// </summary>
public class ChatModelBlabladorQwen : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen3-Next - Latest iteration of the Qwen3 model series.
    /// </summary>
    public static readonly ChatModel ModelQwen3Next = new ChatModel("Qwen3-Next", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3Next"/>
    /// </summary>
    public readonly ChatModel Qwen3Next = ModelQwen3Next;
    
    /// <summary>
    /// Qwen3-VL-30B-A3B-Instruct-FP8 - Vision-language model with 30B parameters in FP8 quantization.
    /// </summary>
    public static readonly ChatModel ModelQwen3VL30BA3BInstructFP8 = new ChatModel("Qwen3-VL-30B-A3B-Instruct-FP8", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelQwen3VL30BA3BInstructFP8"/>
    /// </summary>
    public readonly ChatModel Qwen3VL30BA3BInstructFP8 = ModelQwen3VL30BA3BInstructFP8;
    
    /// <summary>
    /// All known Qwen models from Blablador.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQwen3Next, ModelQwen3VL30BA3BInstructFP8
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelBlabladorQwen()
    {

    }
}

