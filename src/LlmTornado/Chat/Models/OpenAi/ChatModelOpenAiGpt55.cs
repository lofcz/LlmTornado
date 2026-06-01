using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-5.5 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt55 : IVendorModelClassProvider
{
    /// <summary>
    /// GPT-5.5 is OpenAI's newest frontier model for complex professional work.
    /// Supports reasoning.effort: none, low, medium (default), high, and xhigh.
    /// 1.05M context window. Extended prompt caching only (no in-memory caching).
    /// </summary>
    public static readonly ChatModel ModelV55 = new ChatModel("gpt-5.5", LLmProviders.OpenAi, 1_050_000, [ "gpt-5.5-2026-04-23" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV55"/>
    /// </summary>
    public readonly ChatModel V55 = ModelV55;

    /// <summary>
    /// GPT-5.5 Pro uses more compute to think harder and provide consistently better answers.
    /// Available in the Responses API only and supports reasoning.effort: medium, high, and xhigh.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV55Pro = new ChatModel("gpt-5.5-pro", LLmProviders.OpenAi, 1_050_000, [ "gpt-5.5-pro-2026-04-23" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV55Pro"/>
    /// </summary>
    public readonly ChatModel V55Pro = ModelV55Pro;

    /// <summary>
    /// All known GPT-5.5 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV55, ModelV55Pro
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt55()
    {
        
    }
}
