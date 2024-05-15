using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Assistants;
using LlmTornado.Audio;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using LlmTornado.Completions;
using LlmTornado.Embedding;
using LlmTornado.Files;
using LlmTornado.Images;
using LlmTornado.Models;
using LlmTornado.Moderation;
using LlmTornado.Threads;

namespace LlmTornado;

/// <summary>
///     Entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
/// </summary>
public class TornadoApi : ITornadoApi
{
    internal readonly ConcurrentDictionary<LLmProviders, ProviderAuthentication> Authentications = [];
    internal ConcurrentDictionary<LLmProviders, IEndpointProvider> EndpointProviders = [];
    
    private IAssistantsEndpoint? _assistantsEndpoint;
    private IAudioEndpoint? _audioEndpoint;
    private IChatEndpoint? _chat;
    private ICompletionEndpoint? _completionEndpoint;
    private IEmbeddingEndpoint? _embedding;
    private IFilesEndpoint? _files;
    private IImageEditEndpoint? _imageEditEndpoint;
    private IImageGenerationEndpoint? _imageGenerationEndpoint;
    private IModelsEndpoint? _models;
    private IModerationEndpoint? _moderation;
    private IThreadsEndpoint? _threadsEndpoint;

    /// <summary>
    ///     Creates a new Tornado API without any authentication. Use this with self-hosted models.
    /// </summary>
    public TornadoApi()
    {
        
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(LLmProviders provider, string apiKey, string? organization = null)
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey, organization));
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(IEnumerable<ProviderAuthentication> providerKeys)
    {
        foreach (ProviderAuthentication provider in providerKeys)
        {
            Authentications.TryAdd(provider.Provider, provider);
        }
    }

    /// <summary>
    ///     Create a new Tornado API via API key. Use this constructor if in the lifetime of the object only one provider will be used. The API key should match this provider.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="provider">Provider</param>
    public TornadoApi(string apiKey, LLmProviders provider = LLmProviders.OpenAi)
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey));
    }

    /// <summary>
    ///     Create a new OpenAiApi via API key and organization key, suitable for (Azure) OpenAI.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="organizationKey">Organization key</param>
    /// <param name="provider">Provider</param>
    public TornadoApi(string apiKey, string organizationKey, LLmProviders provider = LLmProviders.OpenAi)
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, organizationKey, apiKey));
    }

    /// <summary>
    ///     Gets authentication for a given provider.
    /// </summary>
    /// <returns></returns>
    public ProviderAuthentication? GetProviderAuthentication(LLmProviders provider)
    {
        return Authentications!.GetValueOrDefault(provider, null);
    }

    /// <summary>
    ///     Interceptor
    /// </summary>
    public Func<ChatRequest, ChatResult?, Task>? ChatRequestInterceptor { get; set; }

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and an input image, the model will edit a new image.
    /// </summary>
    public IImageEditEndpoint ImageEdit => _imageEditEndpoint ??= new ImageEditEndpoint(this);

    /// <summary>
    ///     Manages audio operations such as transcipt and translate.
    /// </summary>
    public IAudioEndpoint Audio => _audioEndpoint ??= new AudioEndpoint(this);

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public IAssistantsEndpoint Assistants => _assistantsEndpoint ??= new AssistantsEndpoint(this);

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public IThreadsEndpoint Threads => _threadsEndpoint ??= new ThreadsEndpoint(this);

    /// <summary>
    ///     Base url for Provider. If null, default specified by the provider is used.
    ///     for OpenAI, should be "https://api.openai.com/{0}/{1}"
    ///     for Azure, should be
    ///     "https://(your-resource-name.openai.azure.com/openai/deployments/(deployment-id)/{1}?api-version={0}"
    ///     this will be formatted as {0} = <see cref="ApiVersion" />, {1} = <see cref="EndpointBase.Endpoint" />
    /// </summary>
    public string? ApiUrlFormat { get; set; }

    /// <summary>
    ///     Version of the Rest Api
    /// </summary>
    public string ApiVersion { get; set; } = "v1";
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public IEndpointProvider GetProvider(LLmProviders provider)
    {
        if (EndpointProviders.TryGetValue(provider, out IEndpointProvider? p))
        {
            return p;
        }
        
        IEndpointProvider newProvider = EndpointProviderConverter.CreateProvider(provider, this);
        EndpointProviders.TryAdd(provider, newProvider);
        
        return newProvider;
    }
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known model.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public IEndpointProvider GetProvider(IModel model)
    {
        return GetProvider(model.Provider);
    }

    /// <summary>
    ///     Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way
    ///     you “program” the API to do a task is by simply describing the task in plain english or providing a few written
    ///     examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar
    ///     correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
    /// </summary>
    public ICompletionEndpoint Completions => _completionEndpoint ??= new CompletionEndpoint(this);

    /// <summary>
    ///     The API lets you transform text into a vector (list) of floating point numbers. The distance between two vectors
    ///     measures their relatedness. Small distances suggest high relatedness and large distances suggest low relatedness.
    /// </summary>
    public IEmbeddingEndpoint Embeddings => _embedding ??= new EmbeddingEndpoint(this);

    /// <summary>
    ///     Text generation in the form of chat messages. This interacts with the ChatGPT API.
    /// </summary>
    public IChatEndpoint Chat => _chat ??= new ChatEndpoint(this);

    /// <summary>
    ///     Classify text against the OpenAI Content Policy.
    /// </summary>
    public IModerationEndpoint Moderation => _moderation ??= new ModerationEndpoint(this);

    /// <summary>
    ///     The API endpoint for querying available Engines/models
    /// </summary>
    public IModelsEndpoint Models => _models ??= new ModelsEndpoint(this);

    /// <summary>
    ///     The API lets you do operations with files. You can upload, delete or retrieve files. Files can be used for
    ///     fine-tuning, search, etc.
    /// </summary>
    public IFilesEndpoint Files => _files ??= new FilesEndpoint(this);

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and/or an input image, the model will generate a new
    ///     image.
    /// </summary>
    public IImageGenerationEndpoint ImageGenerations => _imageGenerationEndpoint ??= new ImageGenerationEndpoint(this);
}