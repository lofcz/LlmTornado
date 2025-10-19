using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.DeepInfra;

/// <summary>
/// Flux class models from DeepInfra.
/// </summary>
public class ImageModelDeepInfraFlux : IVendorModelClassProvider
{
    /// <summary>
    /// FLUX.1-Kontext-dev is a state-of-the-art image generation model with exceptional visual quality.
    /// </summary>
    public static readonly ImageModel ModelFlux1KontextDev = new ImageModel("deepinfra-black-forest-labs/FLUX.1-Kontext-dev", "black-forest-labs/FLUX.1-Kontext-dev", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFlux1KontextDev"/>
    /// </summary>
    public readonly ImageModel Flux1KontextDev = ModelFlux1KontextDev;
    
    /// <summary>
    /// FLUX-1-Redux-dev is a variation model that transforms images while preserving style.
    /// </summary>
    public static readonly ImageModel ModelFlux1ReduxDev = new ImageModel("deepinfra-black-forest-labs/FLUX-1-Redux-dev", "black-forest-labs/FLUX-1-Redux-dev", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFlux1ReduxDev"/>
    /// </summary>
    public readonly ImageModel Flux1ReduxDev = ModelFlux1ReduxDev;
    
    /// <summary>
    /// FLUX-1-dev is a high-quality image generation model with breakthrough prompt accuracy.
    /// </summary>
    public static readonly ImageModel ModelFlux1Dev = new ImageModel("deepinfra-black-forest-labs/FLUX-1-dev", "black-forest-labs/FLUX-1-dev", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFlux1Dev"/>
    /// </summary>
    public readonly ImageModel Flux1Dev = ModelFlux1Dev;
    
    /// <summary>
    /// FLUX-1-schnell is an optimized model for fast image generation.
    /// </summary>
    public static readonly ImageModel ModelFlux1Schnell = new ImageModel("deepinfra-black-forest-labs/FLUX-1-schnell", "black-forest-labs/FLUX-1-schnell", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFlux1Schnell"/>
    /// </summary>
    public readonly ImageModel Flux1Schnell = ModelFlux1Schnell;
    
    /// <summary>
    /// FLUX-pro is a professional-grade image generation model.
    /// </summary>
    public static readonly ImageModel ModelFluxPro = new ImageModel("deepinfra-black-forest-labs/FLUX-pro", "black-forest-labs/FLUX-pro", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFluxPro"/>
    /// </summary>
    public readonly ImageModel FluxPro = ModelFluxPro;
    
    /// <summary>
    /// FLUX-1.1-pro is an updated professional-grade image generation model.
    /// </summary>
    public static readonly ImageModel ModelFlux11Pro = new ImageModel("deepinfra-black-forest-labs/FLUX-1.1-pro", "black-forest-labs/FLUX-1.1-pro", LLmProviders.DeepInfra);

    /// <summary>
    /// <inheritdoc cref="ModelFlux11Pro"/>
    /// </summary>
    public readonly ImageModel Flux11Pro = ModelFlux11Pro;
    
    /// <summary>
    /// All known Flux models from DeepInfra.
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ModelFlux1KontextDev, 
        ModelFlux1ReduxDev, 
        ModelFlux1Dev,
        ModelFlux1Schnell,
        ModelFluxPro,
        ModelFlux11Pro
    ];

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ImageModelDeepInfraFlux()
    {
        
    }
}

