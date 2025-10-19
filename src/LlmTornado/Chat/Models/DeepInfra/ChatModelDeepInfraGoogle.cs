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
    /// Gemini 2.5 Pro is a state-of-the-art thinking model with native multimodal capabilities.
    /// </summary>
    public static readonly ChatModel ModelGemini25Pro = new ChatModel("deepinfra-google/gemini-2.5-pro", "google/gemini-2.5-pro", LLmProviders.DeepInfra, 976_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Pro"/>
    /// </summary>
    public readonly ChatModel Gemini25Pro = ModelGemini25Pro;
    
    /// <summary>
    /// Gemini 2.5 Flash is a fast and efficient thinking model with native multimodal capabilities.
    /// </summary>
    public static readonly ChatModel ModelGemini25Flash = new ChatModel("deepinfra-google/gemini-2.5-flash", "google/gemini-2.5-flash", LLmProviders.DeepInfra, 976_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini25Flash"/>
    /// </summary>
    public readonly ChatModel Gemini25Flash = ModelGemini25Flash;
    
    /// <summary>
    /// Gemini 2.0 Flash is a fast and efficient model with native multimodal capabilities.
    /// </summary>
    public static readonly ChatModel ModelGemini20Flash001 = new ChatModel("deepinfra-google/gemini-2.0-flash-001", "google/gemini-2.0-flash-001", LLmProviders.DeepInfra, 976_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemini20Flash001"/>
    /// </summary>
    public readonly ChatModel Gemini20Flash001 = ModelGemini20Flash001;
    
    /// <summary>
    /// Gemma 3 27B introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
    /// </summary>
    public static readonly ChatModel ModelGemma327BIt = new ChatModel("deepinfra-google/gemma-3-27b-it", "google/gemma-3-27b-it", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma327BIt"/>
    /// </summary>
    public readonly ChatModel Gemma327BIt = ModelGemma327BIt;
    
    /// <summary>
    /// Gemma 3 12B introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
    /// </summary>
    public static readonly ChatModel ModelGemma312BIt = new ChatModel("deepinfra-google/gemma-3-12b-it", "google/gemma-3-12b-it", LLmProviders.DeepInfra, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGemma312BIt"/>
    /// </summary>
    public readonly ChatModel Gemma312BIt = ModelGemma312BIt;
    
    /// <summary>
    /// Gemma 3 4B introduces multimodality, supporting vision-language input and text outputs. It handles context windows up to 128k.
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

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelGemini25Pro, ModelGemini25Flash, ModelGemini20Flash001, ModelGemma327BIt, ModelGemma312BIt, ModelGemma34BIt]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraGoogle()
    {

    }
}