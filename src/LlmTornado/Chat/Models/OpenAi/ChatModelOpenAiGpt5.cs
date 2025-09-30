using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// GPT-5 class models from OpenAI.
/// </summary>
public class ChatModelOpenAiGpt5 : IVendorModelClassProvider
{
    /// <summary>
    /// GPT-5-Codex is a version of GPT-5 optimized for agentic coding tasks in Codex or similar environments. 
    /// </summary>
    public static readonly ChatModel ModelV5Codex = new ChatModel("gpt-5-codex", LLmProviders.OpenAi, 400_000, [ ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV5Codex"/>
    /// </summary>
    public readonly ChatModel V5Codex = ModelV5Codex;
    
    /// <summary>
    /// Latest snapshot of GPT-5, currently gpt-5-2025-08-07.
    /// </summary>
    public static readonly ChatModel ModelV5 = new ChatModel("gpt-5", LLmProviders.OpenAi, 400_000, [ "gpt-5-2025-08-07" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV5"/>
    /// </summary>
    public readonly ChatModel V5 = ModelV5;

    /// <summary>
    /// Latest snapshot of GPT-5-Mini, currently gpt-5-mini-2025-08-07.
    /// </summary>
    public static readonly ChatModel ModelV5Mini = new ChatModel("gpt-5-mini", LLmProviders.OpenAi, 400_000, ["gpt-5-mini-2025-08-07"])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV5Mini"/>
    /// </summary>
    public readonly ChatModel V5Mini = ModelV5Mini;
    
    /// <summary>
    /// Latest snapshot of GPT-5-Nano, currently gpt-5-nano-2025-08-07.
    /// </summary>
    public static readonly ChatModel ModelV5Nano = new ChatModel("gpt-5-nano", LLmProviders.OpenAi, 400_000, [ "gpt-5-nano-2025-08-07" ]);

    /// <summary>
    /// <inheritdoc cref="ModelV5Nano"/>
    /// </summary>
    public readonly ChatModel V5Nano = ModelV5Nano;

    /// <summary>
    /// All known GPT-5 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV5, ModelV5Mini, ModelV5Nano, ModelV5Codex
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt5()
    {
        
    }
}