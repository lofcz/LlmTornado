using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.OpenAi;

/// <summary>
/// GPT class models from OpenAI.
/// </summary>
public class ImageModelOpenAiGpt : IVendorModelClassProvider
{
    /// <summary>
    /// Superior instruction following, text rendering, detailed editing, real-world knowledge
    /// </summary>
    public static readonly ImageModel ModelV1Mini = new ImageModel("gpt-image-1-mini", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV1Mini"/>
    /// </summary>
    public readonly ImageModel V1Mini = ModelV1Mini;
    
    /// <summary>
    /// Superior instruction following, text rendering, detailed editing, real-world knowledge
    /// </summary>
    public static readonly ImageModel ModelV1 = new ImageModel("gpt-image-1", LLmProviders.OpenAi);

    /// <summary>
    /// <inheritdoc cref="ModelV1"/>
    /// </summary>
    public readonly ImageModel V1 = ModelV1;
    
    /// <summary>
    /// All known GPT image models from OpenAI.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelV1, ModelV1Mini
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelOpenAiGpt()
    {
        
    }
}