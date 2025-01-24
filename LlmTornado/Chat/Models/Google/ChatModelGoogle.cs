using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Google.
/// </summary>
public class ChatModelGoogle : BaseVendorModelProvider
{
    /// <summary>
    /// Gemini models.
    /// </summary>
    public readonly ChatModelGoogleGemini Gemini = new ChatModelGoogleGemini();
    
    /// <summary>
    /// Experimental Gemini models.
    /// </summary>
    public readonly ChatModelGoogleGeminiExperimental GeminiExperimental = new ChatModelGoogleGeminiExperimental();

    /// <summary>
    /// All known chat models from Google.
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
        ..ChatModelGoogleGemini.ModelsAll,
        ..ChatModelGoogleGeminiExperimental.ModelsAll
    ];
    
    static ChatModelGoogle()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelGoogle()
    {
        AllModels = ModelsAll;
    }
}