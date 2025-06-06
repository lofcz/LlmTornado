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
    /// The Llama 4 collection of models are natively multimodal AI models that enable text and multimodal experiences.
    /// </summary>
    public static readonly ChatModel ModelLlama4Maverick17B128EInstructFP8 = new ChatModel("deepinfra-meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8", "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8", LLmProviders.DeepInfra, 1_024_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama4Maverick17B128EInstructFP8"/>
    /// </summary>
    public readonly ChatModel Llama4Maverick17B128EInstructFP8 = ModelLlama4Maverick17B128EInstructFP8;
    
    /// <summary>
    /// The Llama 4 collection of models are natively multimodal AI models that enable text and multimodal experiences.
    /// </summary>
    public static readonly ChatModel ModelLlama4Scout17B16EInstruct = new ChatModel("deepinfra-meta-llama/Llama-4-Scout-17B-16E-Instruct", "meta-llama/Llama-4-Scout-17B-16E-Instruct", LLmProviders.DeepInfra, 1_024_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama4Scout17B16EInstruct"/>
    /// </summary>
    public readonly ChatModel Llama4Scout17B16EInstruct = ModelLlama4Scout17B16EInstruct;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static readonly List<IModel> ModelsAll =
    [
        ModelLlama4Maverick17B128EInstructFP8,
        ModelLlama4Scout17B16EInstruct,
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraMeta()
    {

    }
}