using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.XAi;

/// <summary>
/// Grok class models from xAI.
/// </summary>
public class ChatModelXAiGrok : IVendorModelClassProvider
{
    /// <summary>
    /// Our latest text model supporting structured outputs, with improved efficiency, speed and capabilities.
    /// </summary>
    public static readonly ChatModel ModelGrok2241212 = new ChatModel("grok-2-1212", LLmProviders.XAi, 200_000, [ "grok-2", "grok-2-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelGrok2241212"/>
    /// </summary>
    public readonly ChatModel Grok2241212 = ModelGrok2241212;
    
    /// <summary>
    /// Our latest text model supporting structured outputs, with improved efficiency, speed and capabilities.
    /// </summary>
    public static readonly ChatModel ModelGrok2Vision241212 = new ChatModel("grok-2-vision-1212", LLmProviders.XAi, 200_000, [ "grok-2-vision", "grok-2-vision-latest" ]);

    /// <summary>
    /// <inheritdoc cref="ModelGrok2Vision241212"/>
    /// </summary>
    public readonly ChatModel Grok2Vision241212 = ModelGrok2Vision241212;
    
    public static readonly List<IModel> ModelsAll = [
        ModelGrok2241212,
        ModelGrok2Vision241212
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrok()
    {
        
    }
}