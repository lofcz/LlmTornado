using System;
using LlmTornado.Chat.Models;
using LlmTornado.Embedding.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for integrating LlmTornado with Microsoft.Extensions.AI.
/// </summary>
public static class TornadoServiceExtensions
{
    /// <summary>
    /// Adds a <see cref="TornadoChatClient"/> as an <see cref="IChatClient"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTornadoChatClient(
        this IServiceCollection services,
        TornadoApi api,
        ChatModel defaultModel,
        Chat.ChatRequest? defaultRequest = null)
    {
        services.AddSingleton<IChatClient>(sp => new TornadoChatClient(api, defaultModel, defaultRequest));
        return services;
    }

    /// <summary>
    /// Adds a <see cref="TornadoChatClient"/> as an <see cref="IChatClient"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTornadoChatClient(
        this IServiceCollection services,
        TornadoApi api,
        string defaultModel,
        Chat.ChatRequest? defaultRequest = null)
    {
        services.AddSingleton<IChatClient>(sp => new TornadoChatClient(api, defaultModel, defaultRequest));
        return services;
    }

    /// <summary>
    /// Adds a <see cref="TornadoEmbeddingGenerator"/> as an <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTornadoEmbeddingGenerator(
        this IServiceCollection services,
        TornadoApi api,
        EmbeddingModel defaultModel,
        int? defaultDimensions = null)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
            sp => new TornadoEmbeddingGenerator(api, defaultModel, defaultDimensions));
        return services;
    }

    /// <summary>
    /// Adds a <see cref="TornadoEmbeddingGenerator"/> as an <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTornadoEmbeddingGenerator(
        this IServiceCollection services,
        TornadoApi api,
        string defaultModel,
        int? defaultDimensions = null)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
            sp => new TornadoEmbeddingGenerator(api, defaultModel, defaultDimensions));
        return services;
    }

    /// <summary>
    /// Creates a new <see cref="TornadoChatClient"/> instance.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    /// <returns>A new chat client instance.</returns>
    public static IChatClient AsChatClient(
        this TornadoApi api,
        ChatModel defaultModel,
        Chat.ChatRequest? defaultRequest = null)
    {
        return new TornadoChatClient(api, defaultModel, defaultRequest);
    }

    /// <summary>
    /// Creates a new <see cref="TornadoChatClient"/> instance.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use.</param>
    /// <param name="defaultRequest">Optional default request settings.</param>
    /// <returns>A new chat client instance.</returns>
    public static IChatClient AsChatClient(
        this TornadoApi api,
        string defaultModel,
        Chat.ChatRequest? defaultRequest = null)
    {
        return new TornadoChatClient(api, defaultModel, defaultRequest);
    }

    /// <summary>
    /// Creates a new <see cref="TornadoEmbeddingGenerator"/> instance.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    /// <returns>A new embedding generator instance.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(
        this TornadoApi api,
        EmbeddingModel defaultModel,
        int? defaultDimensions = null)
    {
        return new TornadoEmbeddingGenerator(api, defaultModel, defaultDimensions);
    }

    /// <summary>
    /// Creates a new <see cref="TornadoEmbeddingGenerator"/> instance.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    /// <returns>A new embedding generator instance.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(
        this TornadoApi api,
        string defaultModel,
        int? defaultDimensions = null)
    {
        return new TornadoEmbeddingGenerator(api, defaultModel, defaultDimensions);
    }
}
