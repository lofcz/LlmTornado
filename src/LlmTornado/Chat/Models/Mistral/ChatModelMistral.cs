using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Mistral;

/// <summary>
/// Known chat models from Mistral.
/// </summary>
public class ChatModelMistral: BaseVendorModelProvider
{
    /// <summary>
    /// All Premier (closed-weights) models.
    /// </summary>
    public readonly ChatModelMistralPremier Premier = new ChatModelMistralPremier();
    
    /// <summary>
    /// All Free (open-weights) models.
    /// </summary>
    public readonly ChatModelMistralFree Free = new ChatModelMistralFree();
    
    /// <summary>
    /// All Research (open-weights) models.
    /// </summary>
    public readonly ChatModelMistralResearch Research = new ChatModelMistralResearch();
    
    /// <summary>
    /// All known chat models from Mistral.
    /// </summary>
    public override List<IModel> AllModels { get; }

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
        ..ChatModelMistralPremier.ModelsAll,
        ..ChatModelMistralFree.ModelsAll,
        ..ChatModelMistralResearch.ModelsAll
    ];
    
    static ChatModelMistral()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelMistral()
    {
        AllModels = ModelsAll;
    }
}