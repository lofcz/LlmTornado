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
    /// Grok is an AI modeled after the Hitchhiker's Guide to the Galaxy. It is intended to answer almost anything and, far harder, even suggest what questions to ask!
    /// </summary>
    public static readonly ChatModel ModelGrokBeta = new ChatModel("grok-beta", LLmProviders.XAi, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelGrokBeta"/>
    /// </summary>
    public readonly ChatModel GrokBeta = ModelGrokBeta;
    
    /// <summary>
    /// In addition to Grok's strong text capabilities, this multimodal model can now process a wide variety of visual information, including documents, diagrams, charts, screenshots, and photographs.
    /// </summary>
    public static readonly ChatModel ModelGrokVisionBeta = new ChatModel("grok-vision-beta", LLmProviders.XAi, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGrokVisionBeta"/>
    /// </summary>
    public readonly ChatModel GrokVisionBeta = ModelGrokVisionBeta;
    
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
        ModelGrok2Vision241212,
        
        ModelGrokBeta,
        ModelGrokVisionBeta
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelXAiGrok()
    {
        
    }
}