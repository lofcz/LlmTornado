using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.OpenAi;

/// <summary>
/// Dalle class models from OpenAI.
/// </summary>
public class ImageModelOpenAiDalle : IVendorModelClassProvider
{
    /// <summary>
    /// The previous DALL·E model released in Nov 2022. The 2nd iteration of DALL·E with more realistic, accurate, and 4x greater resolution images than the original model.
    /// </summary>
    public static readonly ImageModel ModelV2 = new ImageModel("dall-e-2", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV2"/>
    /// </summary>
    public readonly ImageModel V2 = ModelV2;
    
    /// <summary>
    /// The latest DALL·E model released in Nov 2023. 
    /// </summary>
    public static readonly ImageModel ModelV3 = new ImageModel("dall-e-3", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV3"/>
    /// </summary>
    public readonly ImageModel V3 = ModelV3;
    
    /// <summary>
    /// All known Dalle models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelV2, 
        ModelV3
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelOpenAiDalle()
    {
        
    }
}