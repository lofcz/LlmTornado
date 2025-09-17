using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Google models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraGoogle : IVendorModelClassProvider
{
    /// <summary>
    /// Gemma 3 introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
    /// </summary>
    public static readonly ChatModel ModelGemma327BIt = new ChatModel("deepinfra-google/gemma-3-27b-it", "google/gemma-3-27b-it", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma327BIt"/>
    /// </summary>
    public readonly ChatModel Gemma327BIt = ModelGemma327BIt;
    
    /// <summary>
    /// Gemma 3 introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
    /// </summary>
    public static readonly ChatModel ModelGemma312BIt = new ChatModel("deepinfra-google/gemma-3-12b-it", "google/gemma-3-12b-it", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma312BIt"/>
    /// </summary>
    public readonly ChatModel Gemma312BIt = ModelGemma312BIt;
    
    /// <summary>
    /// Gemma 3 introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
    /// </summary>
    public static readonly ChatModel ModelGemma34BIt = new ChatModel("deepinfra-google/gemma-3-4b-it", "google/gemma-3-4b-it", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma34BIt"/>
    /// </summary>
    public readonly ChatModel Gemma34BIt = ModelGemma34BIt;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelGemma327BIt, ModelGemma312BIt, ModelGemma34BIt]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraGoogle()
    {

    }
}