using System.Collections.Generic;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Images.Models.OpenAi;

/// <summary>
/// Known image models from OpenAI.
/// </summary>
public class ImageModelOpenAi : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.OpenAi;
    
    /// <summary>
    /// Dalle models.
    /// </summary>
    public readonly ImageModelOpenAiDalle Dalle = new ImageModelOpenAiDalle();

    /// <summary>
    /// GPT models.
    /// </summary>
    public readonly ImageModelOpenAiGpt Gpt = new ImageModelOpenAiGpt();
    
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
    public static readonly HashSet<string> AllModelsMap = [];
    
    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static readonly List<IModel> ModelsAll = [
        ..ImageModelOpenAiDalle.ModelsAll,
        ..ImageModelOpenAiGpt.ModelsAll
    ];
    
    static ImageModelOpenAi()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ImageModelOpenAi()
    {
        
    }
}