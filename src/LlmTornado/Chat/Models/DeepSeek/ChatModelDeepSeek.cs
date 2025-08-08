using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepSeek;

/// <summary>
/// Known chat models from DeepSeek.
/// </summary>
public class ChatModelDeepSeek: BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.DeepSeek;
    
    /// <summary>
    /// All models.
    /// </summary>
    public readonly ChatModelDeepSeekModels Models = new ChatModelDeepSeekModels();

    /// <summary>
    /// All known chat models from DeepSeek.
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
        ..ChatModelDeepSeekModels.ModelsAll
    ];
    
    static ChatModelDeepSeek()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelDeepSeek()
    {
        
    }
}