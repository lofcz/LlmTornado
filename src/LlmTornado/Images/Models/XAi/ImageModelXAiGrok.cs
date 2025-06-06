using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.XAi;


/// <summary>
/// Grok class models from xAI.
/// </summary>
public class ImageModelXAiGrok : IVendorModelClassProvider
{
    /// <summary>
    /// Our latest image generation model, capable of creating high-quality, detailed images from text prompts with enhanced creativity and precision.
    /// </summary>
    public static readonly ImageModel ModelV2241212 = new ImageModel("grok-2-image-1212", LLmProviders.XAi, [ "grok-2-image", "grok-2-image-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV2241212"/>
    /// </summary>
    public readonly ImageModel V2241212 = ModelV2241212;
    
    /// <summary>
    /// All known Grok models from xAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelV2241212
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelXAiGrok()
    {
        
    }
}