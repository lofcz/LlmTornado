using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Mistral;

/// <summary>
/// All Research (open-weights) models from Mistral.
/// </summary>
public class ChatModelMistralResearch : IVendorModelClassProvider
{
    /// <summary>
    /// Our best multilingual open source model released July 2024.
    /// </summary>
    public static readonly ChatModel ModelMistralNemo = new ChatModel("open-mistral-nemo", LLmProviders.Mistral, 131_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelMistralNemo"/>
    /// </summary>
    public readonly ChatModel MistralNemo = ModelMistralNemo;
    
    /// <summary>
    /// Our first mamba 2 open source model released July 2024. 
    /// </summary>
    public static readonly ChatModel ModelCodestralMamba = new ChatModel("open-codestral-mamba", LLmProviders.Mistral, 256_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelCodestralMamba"/>
    /// </summary>
    public readonly ChatModel CodestralMamba = ModelCodestralMamba;
    
    /// <summary>
    /// All known Premier models from Mistral.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelMistralNemo, ModelCodestralMamba]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelMistralResearch()
    {

    }
}