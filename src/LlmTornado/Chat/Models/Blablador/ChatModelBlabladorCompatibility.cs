using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models;

/// <summary>
/// OpenAI compatibility aliases for Blablador.
/// These allow tools like Langchain that expect OpenAI model names to work with Blablador.
/// </summary>
public class ChatModelBlabladorCompatibility : IVendorModelClassProvider
{
    /// <summary>
    /// gpt-3.5-turbo - OpenAI compatibility alias pointing to a Blablador model.
    /// </summary>
    public static readonly ChatModel ModelGpt35Turbo = new ChatModel("gpt-3.5-turbo", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelGpt35Turbo"/>
    /// </summary>
    public readonly ChatModel Gpt35Turbo = ModelGpt35Turbo;
    
    /// <summary>
    /// text-davinci-003 - OpenAI compatibility alias pointing to a Blablador model.
    /// </summary>
    public static readonly ChatModel ModelTextDavinci003 = new ChatModel("text-davinci-003", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelTextDavinci003"/>
    /// </summary>
    public readonly ChatModel TextDavinci003 = ModelTextDavinci003;
    
    /// <summary>
    /// text-embedding-ada-002 - OpenAI compatibility alias for embeddings.
    /// </summary>
    public static readonly ChatModel ModelTextEmbeddingAda002 = new ChatModel("text-embedding-ada-002", LLmProviders.Blablador, 128_000);
    
    /// <summary>
    /// <inheritdoc cref="ModelTextEmbeddingAda002"/>
    /// </summary>
    public readonly ChatModel TextEmbeddingAda002 = ModelTextEmbeddingAda002;
    
    /// <summary>
    /// All OpenAI compatibility aliases from Blablador.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelGpt35Turbo, ModelTextDavinci003, ModelTextEmbeddingAda002
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelBlabladorCompatibility()
    {

    }
}

