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
    /// GPT-5 pro uses more compute to think harder and provide consistently better answers.
    /// </summary>
    public static readonly ChatModel ModelV5Pro = new ChatModel("gpt-5-pro", LLmProviders.OpenAi, 400_000, [ "gpt-5-pro-2025-10-06" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV5Pro"/>
    /// </summary>
    public readonly ChatModel V5Pro = ModelV5Pro;
    
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
    /// A cost-efficient version of GPT Audio. It accepts audio inputs and outputs, and can be used in the Chat Completions REST API.
    /// </summary>
    public static readonly ChatModel ModelAudio = new ChatModel("gpt-audio", LLmProviders.OpenAi, 128_000, [ "gpt-audio-2025-08-28" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Chat ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelAudio"/>
    /// </summary>
    public readonly ChatModel Audio = ModelAudio;
    
    /// <summary>
    /// A cost-efficient version of GPT Audio. It accepts audio inputs and outputs, and can be used in the Chat Completions REST API.
    /// </summary>
    public static readonly ChatModel ModelAudioMini = new ChatModel("gpt-audio-mini", LLmProviders.OpenAi, 128_000, [ "gpt-audio-mini-2025-10-06" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Chat ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelAudioMini"/>
    /// </summary>
    public readonly ChatModel AudioMini = ModelAudioMini;
    
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
        ModelV5, ModelV5Mini, ModelV5Nano, ModelV5Codex, ModelV5Pro, ModelAudioMini
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt5()
    {
        
    }
}