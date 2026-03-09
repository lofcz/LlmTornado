using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.DeepInfra;

/// <summary>
/// Anthropic Claude models from DeepInfra.
/// </summary>
public class ChatModelDeepInfraAnthropic : IVendorModelClassProvider
{
    /// <summary>
    /// Claude 4 Opus is a highly performant model built for complex reasoning and advanced coding.
    /// </summary>
    public static readonly ChatModel ModelClaude4Opus = new ChatModel("deepinfra-anthropic/claude-4-opus", "anthropic/claude-4-opus", LLmProviders.DeepInfra, 195_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelClaude4Opus"/>
    /// </summary>
    public readonly ChatModel Claude4Opus = ModelClaude4Opus;
    
    /// <summary>
    /// Claude 4 Sonnet is a balanced model built for complex reasoning with improved speed.
    /// </summary>
    public static readonly ChatModel ModelClaude4Sonnet = new ChatModel("deepinfra-anthropic/claude-4-sonnet", "anthropic/claude-4-sonnet", LLmProviders.DeepInfra, 195_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelClaude4Sonnet"/>
    /// </summary>
    public readonly ChatModel Claude4Sonnet = ModelClaude4Sonnet;
    
    /// <summary>
    /// Known models.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelClaude4Opus, ModelClaude4Sonnet]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelDeepInfraAnthropic()
    {

    }
}

