using System.Collections.Generic;
using LlmTornado.Chat.Models.XAi;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Perplexity;

/// <summary>
/// Known chat models from Perplexity.
/// </summary>
public class ChatModelPerplexity : BaseVendorModelProvider
{
    /// <summary>
    /// Sonar models.
    /// </summary>
    public readonly ChatModelPerplexitySonar Sonar = new ChatModelPerplexitySonar();

    /// <summary>
    /// All known chat models from xAI.
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
    public static readonly List<IModel> ModelsAll =
    [
        ..ChatModelXAiGrok.ModelsAll
    ];

    static ChatModelPerplexity()
    {
        ModelsAll.ForEach(x => { AllModelsMap.Add(x.Name); });
    }

    internal ChatModelPerplexity()
    {
        AllModels = ModelsAll;
    }
}
