using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-5.3 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt53 : IVendorModelClassProvider
{
    /// <summary>
    /// Latest snapshot of GPT-5.3 Instant, the model powering ChatGPT everyday conversations.
    /// Released Mar 3, 2026; snapshot updated Mar 16, 2026.
    /// 400,000 context window, 128,000 max output tokens.
    /// </summary>
    public static readonly ChatModel ModelV53ChatLatest = new ChatModel("gpt-5.3-chat-latest", LLmProviders.OpenAi, 400_000, [])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV53ChatLatest"/>
    /// </summary>
    public readonly ChatModel V53ChatLatest = ModelV53ChatLatest;

    /// <summary>
    /// All known GPT-5.3 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV53ChatLatest
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt53()
    {
        
    }
}
