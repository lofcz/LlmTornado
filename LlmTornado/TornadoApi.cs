using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Assistants;
using LlmTornado.Audio;
using LlmTornado.Caching;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
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
using LlmTornado.VectorStores;

namespace LlmTornado;

/// <summary>
///     Entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
/// </summary>
public class TornadoApi
{
    internal readonly ConcurrentDictionary<LLmProviders, ProviderAuthentication> Authentications = [];
    internal readonly ConcurrentDictionary<LLmProviders, IEndpointProvider> EndpointProviders = [];

    private readonly Lazy<AssistantsEndpoint> assistants;
    private readonly Lazy<AudioEndpoint> audio;
    private readonly Lazy<ChatEndpoint> chat;
    private readonly Lazy<CompletionEndpoint> completion;
    private readonly Lazy<EmbeddingEndpoint> embedding;
    private readonly Lazy<FilesEndpoint> files;
    private readonly Lazy<ImageEditEndpoint> imageEdit;
    private readonly Lazy<ImageGenerationEndpoint> imageGeneration;
    private readonly Lazy<ModelsEndpoint> models;
    private readonly Lazy<ModerationEndpoint> moderation;
    private readonly Lazy<ThreadsEndpoint> threads;
    private readonly Lazy<VectorStoresEndpoint> vectorStores;
    private readonly Lazy<CachingEndpoint> caching;

    /// <summary>
    ///     If true, the API will throw exceptions for non-200 responses.
    /// </summary>
    internal bool httpStrict;
    
    /// <summary>
    ///     Creates a new Tornado API without any authentication. Use this with self-hosted models.
    /// </summary>
    public TornadoApi()
    {
        assistants = new Lazy<AssistantsEndpoint>(() => new AssistantsEndpoint(this));
        audio = new Lazy<AudioEndpoint>(() => new AudioEndpoint(this));
        chat = new Lazy<ChatEndpoint>(() => new ChatEndpoint(this));
        completion = new Lazy<CompletionEndpoint>(() => new CompletionEndpoint(this));
        embedding = new Lazy<EmbeddingEndpoint>(() => new EmbeddingEndpoint(this));
        files = new Lazy<FilesEndpoint>(() => new FilesEndpoint(this));
        imageEdit = new Lazy<ImageEditEndpoint>(() => new ImageEditEndpoint(this));
        imageGeneration = new Lazy<ImageGenerationEndpoint>(() => new ImageGenerationEndpoint(this));
        models = new Lazy<ModelsEndpoint>(() => new ModelsEndpoint(this));
        moderation = new Lazy<ModerationEndpoint>(() => new ModerationEndpoint(this));
        threads = new Lazy<ThreadsEndpoint>(() => new ThreadsEndpoint(this));
        vectorStores = new Lazy<VectorStoresEndpoint>(() => new VectorStoresEndpoint(this));
        caching = new Lazy<CachingEndpoint>(() => new CachingEndpoint(this));
    }

    /// <summary>
    ///     Creates a new Tornado API for self-hosted / custom providers, such as Ollama and vLLM.<br/>
    ///     For Ollama use "http://localhost:11434" (by default).
    ///     For vLLM use "http://localhost:8000" (by default).
    /// </summary>
    /// <param name="serverUri">Uri of the server. Tokens {0} and {1} are available for endpoint and action respectively. If provided values doesn't use neither, format /{0}/{1} is used automatically.</param>
    public TornadoApi(Uri serverUri) : this()
    {
        string serverUriStr = serverUri.ToString();

        if (!serverUriStr.Contains("{0}"))
        {
            serverUriStr = $"{serverUriStr}{(serverUriStr.EndsWith('/') ? string.Empty : "/")}{{0}}/{{1}}";
        }

        ApiUrlFormat = serverUriStr;
    }
    
    /// <summary>
    ///     Creates a new Tornado API for self-hosted / custom providers, such as Ollama and vLLM.<br/>
    ///     For Ollama use "http://localhost:11434" (by default).
    ///     For vLLM use "http://localhost:8000" (by default).
    /// </summary>
    /// <param name="serverUri">Uri of the server. Tokens {0} and {1} are available for endpoint and action respectively. If provided values doesn't use neither, format /{0}/{1} is used automatically.</param>
    /// <param name="apiKey">API key to use</param>
    /// <param name="provider">Provider to use</param>
    public TornadoApi(Uri serverUri, string apiKey, LLmProviders provider = LLmProviders.Custom) : this()
    {
        string serverUriStr = serverUri.ToString();

        if (!serverUriStr.Contains("{0}"))
        {
            serverUriStr = $"{serverUriStr}{(serverUriStr.EndsWith('/') ? string.Empty : "/")}{{0}}/{{1}}";
        }

        ApiUrlFormat = serverUriStr;
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey));
    }

    /// <summary>
    /// Creates an instance from a custom provider.<br/>
    /// This constructor is needed only for custom deployments where setting <see cref="ApiUrlFormat"/> is not sufficient.
    /// </summary>
    /// <param name="provider">The provider to use</param>
    public TornadoApi(IEndpointProvider provider) : this()
    {
        EnlistProvider(provider);
    }
    
    /// <summary>
    /// Creates an instance from a custom provider.<br/>
    /// This constructor is needed only for custom deployments where setting <see cref="ApiUrlFormat"/> is not sufficient.
    /// </summary>
    /// <param name="providers">Providers to use</param>
    public TornadoApi(IEnumerable<IEndpointProvider> providers) : this()
    {
        foreach (IEndpointProvider provider in providers)
        {
            EnlistProvider(provider);
        }
    }

    void EnlistProvider(IEndpointProvider provider)
    {
        provider.Api = this;

        if (provider.Auth is not null)
        {
            provider.Auth.Provider = provider.Provider;
            Authentications.TryAdd(provider.Provider, provider.Auth);
        }
        
        EndpointProviders.TryAdd(provider.Provider, provider);
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(LLmProviders provider, string apiKey, string organization) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey, organization));
    }
    
    /// <summary>
    ///     Creates a new Tornado API with a specific provider authentication. Use when the API will be used only with a single provider.
    /// </summary>
    public TornadoApi(IEnumerable<ProviderAuthentication> providerKeys) : this()
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
    public TornadoApi(string apiKey, LLmProviders provider = LLmProviders.OpenAi) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey));
    }

    /// <summary>
    ///     Create a new OpenAiApi via API key and organization key, suitable for (Azure) OpenAI.
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="organizationKey">Organization key</param>
    /// <param name="provider">Provider</param>
    public TornadoApi(string apiKey, string organizationKey, LLmProviders provider = LLmProviders.OpenAi) : this()
    {
        Authentications.TryAdd(provider, new ProviderAuthentication(provider, apiKey, organizationKey));
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

    internal IEndpointProvider ResolveProvider(LLmProviders? userSignalledProvider = null)
    {
        return GetProvider(userSignalledProvider ?? GetFirstAuthenticatedProvider());
    }
    
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

        if (Authentications.TryGetValue(provider, out _))
        {
            IEndpointProvider newProvider = EndpointProviderConverter.CreateProvider(provider, this);
            EndpointProviders.TryAdd(provider, newProvider);
            return newProvider;   
        }
        
        if (!EndpointProviders.IsEmpty)
        {
            return EndpointProviders.FirstOrDefault().Value;
        }

        if (!Authentications.IsEmpty)
        {
            KeyValuePair<LLmProviders, ProviderAuthentication> auth = Authentications.FirstOrDefault();
            IEndpointProvider newDefaultProvider = EndpointProviderConverter.CreateProvider(provider, this);
            newDefaultProvider.Auth = auth.Value;
            EndpointProviders.TryAdd(provider, newDefaultProvider);
            return newDefaultProvider;   
        }
        
        IEndpointProvider newFallbackProvider = EndpointProviderConverter.CreateProvider(provider, this);
        EndpointProviders.TryAdd(provider, newFallbackProvider);
        return newFallbackProvider;   
    }

    /// <summary>
    /// Returns first authenticated provider. This is order-unstable as the underlying storage is a dictionary - if more than one provider is authenticated, you should explicitly set provider in your requests.
    /// </summary>
    /// <returns>Returns first authenticated provider, or OpenAi as fallback</returns>
    public LLmProviders GetFirstAuthenticatedProvider()
    {
        return Authentications.IsEmpty ? LLmProviders.OpenAi : Authentications.FirstOrDefault().Key;
    }
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known model.
    /// </summary>
    public IEndpointProvider GetProvider(IModel model)
    {
        return GetProvider(model.Provider);
    }
    
    /// <summary>
    /// Returns a concrete implementation of endpoint provider for a given known model.
    /// </summary>
    public IEndpointProvider GetProvider(ChatModel model)
    {
        if (model.Provider is LLmProviders.Unknown)
        {
            IModel? match = model.ApiName is null ? null : ChatModel.AllModelsApiMap!.GetValueOrDefault(model.ApiName, null);
            match ??= ChatModel.AllModelsMap!.GetValueOrDefault(model.Name, null);
            match ??= ChatModel.AllModelsApiMap!.GetValueOrDefault(model.Name, null);
            
            if (match is not null)
            {
                model.Provider = match.Provider;
            }
        }
        
        return GetProvider(model.Provider);
    }

    /// <summary>
    ///     Interceptor
    /// </summary>
    public Func<ChatRequest, ChatResult?, Task>? ChatRequestInterceptor { get; set; }

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and an input image, the model will edit a new image.
    /// </summary>
    public ImageEditEndpoint ImageEdit => imageEdit.Value;

    /// <summary>
    ///     Manages audio operations such as transcipt and translate.
    /// </summary>
    public AudioEndpoint Audio => audio.Value;

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public AssistantsEndpoint Assistants => assistants.Value;

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public ThreadsEndpoint Threads => threads.Value;

    /// <summary>
    ///     Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way
    ///     you “program” the API to do a task is by simply describing the task in plain english or providing a few written
    ///     examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar
    ///     correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
    /// </summary>
    public CompletionEndpoint Completions => completion.Value;

    /// <summary>
    ///     The API lets you transform text into a vector (list) of floating point numbers. The distance between two vectors
    ///     measures their relatedness. Small distances suggest high relatedness and large distances suggest low relatedness.
    /// </summary>
    public EmbeddingEndpoint Embeddings => embedding.Value;

    /// <summary>
    ///     Text generation in the form of chat messages. This interacts with the ChatGPT API.
    /// </summary>
    public ChatEndpoint Chat => chat.Value;

    /// <summary>
    ///     Classify text against the OpenAI Content Policy.
    /// </summary>
    public ModerationEndpoint Moderation => moderation.Value;

    /// <summary>
    ///     The API endpoint for querying available Engines/models.
    /// </summary>
    public ModelsEndpoint Models => models.Value;

    /// <summary>
    ///     The API lets you do operations with files. You can upload, delete or retrieve files. Files can be used for
    ///     fine-tuning, search, etc.
    /// </summary>
    public FilesEndpoint Files => files.Value;

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and/or an input image, the model will generate a new
    ///     image.
    /// </summary>
    public ImageGenerationEndpoint ImageGenerations => imageGeneration.Value;
    
    /// <summary>
    ///     The API lets you do operations with vector stores on OpenAI API.
    /// </summary>
    public VectorStoresEndpoint VectorStores => vectorStores.Value;
    
    /// <summary>
    ///     The API lets you cache messages. Use only with <see cref="LLmProviders.Google"/>
    /// </summary>
    public CachingEndpoint Caching => caching.Value;
}