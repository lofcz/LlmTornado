using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models provided by Blablador (Helmholtz).
/// </summary>
public class ChatModelBlablador : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Blablador;
    
    /// <summary>
    /// Model aliases for easy selection. Aliases point to current recommended models.
    /// </summary>
    public readonly ChatModelBlabladorAliases Aliases = new ChatModelBlabladorAliases();
    
    /// <summary>
    /// Qwen family models.
    /// </summary>
    public readonly ChatModelBlabladorQwen Qwen = new ChatModelBlabladorQwen();
    
    /// <summary>
    /// Mistral AI models.
    /// </summary>
    public readonly ChatModelBlabladorMistral Mistral = new ChatModelBlabladorMistral();
    
    /// <summary>
    /// Other available models.
    /// </summary>
    public readonly ChatModelBlabladorOthers Others = new ChatModelBlabladorOthers();
    
    /// <summary>
    /// OpenAI compatibility aliases for tools like Langchain.
    /// </summary>
    public readonly ChatModelBlabladorCompatibility Compatibility = new ChatModelBlabladorCompatibility();

    /// <summary>
    /// All known chat models hosted by Blablador.
    /// Note: Blablador uses dynamic models. Use the Models API endpoint to fetch the current list.
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
        ..ChatModelBlabladorAliases.ModelsAll,
        ..ChatModelBlabladorQwen.ModelsAll,
        ..ChatModelBlabladorMistral.ModelsAll,
        ..ChatModelBlabladorOthers.ModelsAll,
        ..ChatModelBlabladorCompatibility.ModelsAll
    ]);
    
    internal ChatModelBlablador()
    {
      
    }
}

