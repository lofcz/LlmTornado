using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-5.4 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt54 : IVendorModelClassProvider
{
    /// <summary>
    /// GPT-5.4 is OpenAI's frontier model for complex professional work.
    /// Supports reasoning.effort: none (default), low, medium, high, and xhigh.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV54 = new ChatModel("gpt-5.4", LLmProviders.OpenAi, 1_050_000, [])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV54"/>
    /// </summary>
    public readonly ChatModel V54 = ModelV54;

    /// <summary>
    /// GPT-5.4 Pro uses more compute to think harder and provide consistently better answers.
    /// Available in the Responses API only and supports reasoning.effort: medium, high, and xhigh.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV54Pro = new ChatModel("gpt-5.4-pro", LLmProviders.OpenAi, 1_050_000, [])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV54Pro"/>
    /// </summary>
    public readonly ChatModel V54Pro = ModelV54Pro;

    /// <summary>
    /// All known GPT-5.4 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV54, ModelV54Pro
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt54()
    {
        
    }
}