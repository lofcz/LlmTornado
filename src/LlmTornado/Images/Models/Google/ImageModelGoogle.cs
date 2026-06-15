using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.Google;

/// <summary>
/// Known image models from Google.
/// </summary>
public class ImageModelGoogle : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Google;
    
    /// <summary>
    /// Imagen models.
    /// </summary>
    public readonly ImageModelGoogleImagen Imagen = new ImageModelGoogleImagen();
    
    /// <summary>
    /// Imagen preview models.
    /// </summary>
    public readonly ImageModelGoogleImagenPreview ImagenPreview = new ImageModelGoogleImagenPreview();

    /// <summary>
    /// Gemini native image models (generateContent-based).
    /// </summary>
    public readonly ImageModelGoogleGemini Gemini = new ImageModelGoogleGemini();
    
    /// <summary>
    /// All known image models from OpenAI.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;
    
    /// <summary>
    /// Checks whether the model is owned by the provider.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;
    
    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];
        ModelsAll.ForEach(x => { map.Add(x.Name); });
        return map;
    });
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;
    
    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ..ImageModelGoogleImagen.ModelsAll,
        ..ImageModelGoogleImagenPreview.ModelsAll,
        ..ImageModelGoogleGemini.ModelsAll
    ]);
    
    internal ImageModelGoogle()
    {
        
    }
}