using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// NVIDIA Nemotron models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraNemotron : IVendorModelClassProvider
{
    /// <summary>
    /// Llama-3.1-Nemotron-70B-Instruct is customized for efficiency, accuracy, and specialized workloads.
    /// </summary>
    public static readonly ChatModel ModelLlama31Nemotron70BInstruct = new ChatModel("deepinfra-nvidia/Llama-3.1-Nemotron-70B-Instruct", "nvidia/Llama-3.1-Nemotron-70B-Instruct", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama31Nemotron70BInstruct"/>
    /// </summary>
    public readonly ChatModel Llama31Nemotron70BInstruct = ModelLlama31Nemotron70BInstruct;
    
    /// <summary>
    /// Llama-3.3-Nemotron-Super-49B-v1.5 is a high-performance model optimized for specialized workloads.
    /// </summary>
    public static readonly ChatModel ModelLlama33NemotronSuper49BV15 = new ChatModel("deepinfra-nvidia/Llama-3.3-Nemotron-Super-49B-v1.5", "nvidia/Llama-3.3-Nemotron-Super-49B-v1.5", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelLlama33NemotronSuper49BV15"/>
    /// </summary>
    public readonly ChatModel Llama33NemotronSuper49BV15 = ModelLlama33NemotronSuper49BV15;
    
    /// <summary>
    /// NVIDIA-Nemotron-Nano-9B-v2 is a compact and efficient model for edge deployments.
    /// </summary>
    public static readonly ChatModel ModelNVIDIANemotronNano9BV2 = new ChatModel("deepinfra-nvidia/NVIDIA-Nemotron-Nano-9B-v2", "nvidia/NVIDIA-Nemotron-Nano-9B-v2", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelNVIDIANemotronNano9BV2"/>
    /// </summary>
    public readonly ChatModel NVIDIANemotronNano9BV2 = ModelNVIDIANemotronNano9BV2;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelLlama31Nemotron70BInstruct, ModelLlama33NemotronSuper49BV15, ModelNVIDIANemotronNano9BV2]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraNemotron()
    {

    }
}

