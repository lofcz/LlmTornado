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
    public static readonly ChatModel ModelV54 = new ChatModel("gpt-5.4", LLmProviders.OpenAi, 1_050_000, [ "gpt-5.4-2026-03-05" ])
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
    public static readonly ChatModel ModelV54Pro = new ChatModel("gpt-5.4-pro", LLmProviders.OpenAi, 1_050_000, [ "gpt-5.4-pro-2026-03-05" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV54Pro"/>
    /// </summary>
    public readonly ChatModel V54Pro = ModelV54Pro;

    /// <summary>
    /// GPT-5.4 mini brings GPT-5.4-class capabilities to a faster, more efficient model for high-volume workloads.
    /// Supports tool search, built-in computer use, and compaction.
    /// 400K context window.
    /// </summary>
    public static readonly ChatModel ModelV54Mini = new ChatModel("gpt-5.4-mini", LLmProviders.OpenAi, 400_000, [ "gpt-5.4-mini-2026-03-17" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV54Mini"/>
    /// </summary>
    public readonly ChatModel V54Mini = ModelV54Mini;

    /// <summary>
    /// GPT-5.4 nano is optimized for simple high-volume tasks where speed and cost matter most.
    /// Supports compaction, but does not support tool search or computer use.
    /// 400K context window.
    /// </summary>
    public static readonly ChatModel ModelV54Nano = new ChatModel("gpt-5.4-nano", LLmProviders.OpenAi, 400_000, [ "gpt-5.4-nano-2026-03-17" ])
    {
        EndpointCapabilities = [ ChatModelEndpointCapabilities.Responses, ChatModelEndpointCapabilities.Chat, ChatModelEndpointCapabilities.Batch ]
    };

    /// <summary>
    /// <inheritdoc cref="ModelV54Nano"/>
    /// </summary>
    public readonly ChatModel V54Nano = ModelV54Nano;

    /// <summary>
    /// All known GPT-5.4 models from OpenAI.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelV54, ModelV54Pro, ModelV54Mini, ModelV54Nano
    ]);
    
    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;
    
    internal ChatModelOpenAiGpt54()
    {
        
    }
}