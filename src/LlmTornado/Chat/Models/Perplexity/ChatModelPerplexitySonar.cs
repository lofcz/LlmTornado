using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Perplexity;

/// <summary>
/// Sonar class models from Perplexity.
/// </summary>
public class ChatModelPerplexitySonar : IVendorModelClassProvider
{
    /// <summary>
    /// Advanced search offering with grounding, supporting complex queries and follow-ups.
    /// </summary>
    public static readonly ChatModel ModelPro = new ChatModel("sonar-pro", LLmProviders.Perplexity, 128_000);

    /// <summary>
    /// <inheritdoc cref="ModelPro"/>
    /// </summary>
    public readonly ChatModel Pro = ModelPro;
    
    /// <summary>
    /// Latest Sonar snapshot.
    /// </summary>
    public static readonly ChatModel ModelDefault = new ChatModel("sonar", LLmProviders.Perplexity, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDefault"/>
    /// </summary>
    public readonly ChatModel Default = ModelDefault;
    
    /// <summary>
    /// Expert-level research model conducting exhaustive searches and generating comprehensive reports.
    /// </summary>
    public static readonly ChatModel ModelDeepResearch = new ChatModel("sonar-deep-research", LLmProviders.Perplexity, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelDeepResearch"/>
    /// </summary>
    public readonly ChatModel DeepResearch = ModelDeepResearch;
    
    /// <summary>
    /// Premier reasoning offering powered by DeepSeek R1 with Chain of Thought (CoT).
    /// </summary>
    public static readonly ChatModel ModelReasoningPro = new ChatModel("sonar-reasoning-pro", LLmProviders.Perplexity, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelReasoningPro"/>
    /// </summary>
    public readonly ChatModel ReasoningPro = ModelReasoningPro;
    
    /// <summary>
    /// Fast, real-time reasoning model designed for quick problem-solving with search.
    /// </summary>
    public static readonly ChatModel ModelReasoning = new ChatModel("sonar-reasoning", LLmProviders.Perplexity, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelReasoning"/>
    /// </summary>
    public readonly ChatModel Reasoning = ModelReasoning;
    
    public static readonly List<IModel> ModelsAll = [
        ModelPro,
        ModelDefault,
        ModelDeepResearch,
        ModelReasoningPro,
        ModelReasoning
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelPerplexitySonar()
    {
        
    }
}