using System.Collections.Generic;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Known chat models from Anthropic.
/// </summary>
public class ChatModelAnthropic : BaseVendorModelProvider
{
    /// <summary>
    /// Claude 3 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude3 Claude3 = new ChatModelAnthropicClaude3();
    
    /// <summary>
    /// Claude 3.5 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude35 Claude35 = new ChatModelAnthropicClaude35();
    
    /// <summary>
    /// Claude 3.7 models.
    /// </summary>
    public readonly ChatModelAnthropicClaude37 Claude37 = new ChatModelAnthropicClaude37();
    
    /// <summary>
    /// All known chat models from Anthropic.
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
        ..ChatModelAnthropicClaude3.ModelsAll,
        ..ChatModelAnthropicClaude35.ModelsAll,
        ..ChatModelAnthropicClaude37.ModelsAll
    ];
    
    static ChatModelAnthropic()
    {
        ModelsAll.ForEach(x =>
        {
            AllModelsMap.Add(x.Name);
        });
    }
    
    internal ChatModelAnthropic()
    {
        AllModels = ModelsAll;
    }
}