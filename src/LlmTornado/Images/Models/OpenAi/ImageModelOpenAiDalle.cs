using System;
using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.OpenAi;

/// <summary>
/// DALL·E class models from OpenAI. Removed from the OpenAI API on May 12, 2026.
/// </summary>
[Obsolete("DALL·E models were removed from the OpenAI API on May 12, 2026. Use ImageModelOpenAiGpt instead.")]
public class ImageModelOpenAiDalle : IVendorModelClassProvider
{
    /// <summary>
    /// The previous DALL·E model released in Nov 2022. The 2nd iteration of DALL·E with more realistic, accurate, and 4x greater resolution images than the original model.
    /// </summary>
    [Obsolete("Removed from the OpenAI API on May 12, 2026. Use ImageModel.OpenAi.Gpt.V2, V1, or V1Mini instead.")]
    public static readonly ImageModel ModelV2 = new ImageModel("dall-e-2", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV2"/>
    /// </summary>
    [Obsolete("Removed from the OpenAI API on May 12, 2026. Use ImageModel.OpenAi.Gpt.V2, V1, or V1Mini instead.")]
    public readonly ImageModel V2 = ModelV2;
    
    /// <summary>
    /// The latest DALL·E model released in Nov 2023. 
    /// </summary>
    [Obsolete("Removed from the OpenAI API on May 12, 2026. Use ImageModel.OpenAi.Gpt.V2, V1, or V1Mini instead.")]
    public static readonly ImageModel ModelV3 = new ImageModel("dall-e-3", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    [Obsolete("Removed from the OpenAI API on May 12, 2026. Use ImageModel.OpenAi.Gpt.V2, V1, or V1Mini instead.")]
    public readonly ImageModel V3 = ModelV3;
    
    /// <summary>
    /// All known Dalle models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;
    
    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV2, 
        ModelV3
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelOpenAiDalle()
    {
        
    }
}