using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-5.6 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt56 : IVendorModelClassProvider
{
    /// <summary>
    /// GPT-5.6 is an alias that routes to GPT-5.6 Sol, the flagship model for complex reasoning and coding.
    /// Supports reasoning.effort: none, low, medium (default), high, xhigh, and max.
    /// Pro mode is enabled via reasoning.mode: "pro" rather than a separate model slug.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV56 = new ChatModel("gpt-5.6", LLmProviders.OpenAi, 1_050_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV56"/>
    /// </summary>
    public readonly ChatModel V56 = ModelV56;

    /// <summary>
    /// GPT-5.6 Sol is the frontier model in the GPT-5.6 family for complex professional work.
    /// Supports reasoning.effort: none, low, medium (default), high, xhigh, and max.
    /// Pro mode is enabled via reasoning.mode: "pro" rather than a separate model slug.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV56Sol = new ChatModel("gpt-5.6-sol", LLmProviders.OpenAi, 1_050_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV56Sol"/>
    /// </summary>
    public readonly ChatModel V56Sol = ModelV56Sol;

    /// <summary>
    /// GPT-5.6 Terra balances intelligence and cost for everyday professional work.
    /// Supports reasoning.effort: none, low, medium (default), high, xhigh, and max.
    /// Pro mode is enabled via reasoning.mode: "pro" rather than a separate model slug.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV56Terra = new ChatModel("gpt-5.6-terra", LLmProviders.OpenAi, 1_050_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV56Terra"/>
    /// </summary>
    public readonly ChatModel V56Terra = ModelV56Terra;

    /// <summary>
    /// GPT-5.6 Luna is optimized for cost-sensitive, high-volume workloads.
    /// Supports reasoning.effort: none, low, medium (default), high, xhigh, and max.
    /// Pro mode is enabled via reasoning.mode: "pro" rather than a separate model slug.
    /// 1.05M context window.
    /// </summary>
    public static readonly ChatModel ModelV56Luna = new ChatModel("gpt-5.6-luna", LLmProviders.OpenAi, 1_050_000)
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV56Luna"/>
    /// </summary>
    public readonly ChatModel V56Luna = ModelV56Luna;

    /// <summary>
    /// All known GPT-5.6 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV56, ModelV56Sol, ModelV56Terra, ModelV56Luna
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt56()
    {
        
    }
}

