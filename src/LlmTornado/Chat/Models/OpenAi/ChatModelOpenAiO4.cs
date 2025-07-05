using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// O4 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiO4 : IVendorModelClassProvider
{
    /// <summary>
    /// o4-mini-deep-research is our faster, more affordable deep research model—ideal for tackling complex, multi-step research tasks. It can search and synthesize information from across the internet as well as from your own data, brought in through MCP connectors.
    /// </summary>
    public static readonly ChatModel ModelV4MiniDeepResearch = new ChatModel("o4-mini-deep-research", LLmProviders.OpenAi, 200_000, [ "o4-mini-deep-research-2025-06-26" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV4MiniDeepResearch"/>
    /// </summary>
    public readonly ChatModel V4MiniDeepResearch = ModelV4MiniDeepResearch;
    
    /// <summary>
    /// o4-mini is our latest small o-series model. It's optimized for fast, effective reasoning with exceptionally efficient performance in coding and visual tasks.
    /// </summary>
    public static readonly ChatModel ModelV4Mini = new ChatModel("o4-mini", LLmProviders.OpenAi, 200_000, [ "o4-mini-2025-04-16" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV4Mini"/>
    /// </summary>
    public readonly ChatModel V4Mini = ModelV4Mini;
    
    public static readonly List<IModel> ModelsAll = [
        ModelV4Mini,
        ModelV4MiniDeepResearch
    ];
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiO4()
    {
        
    }
}